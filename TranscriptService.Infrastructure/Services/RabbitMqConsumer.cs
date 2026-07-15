using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TranscriptService.Domain.ValueObjects;
using TranscriptService.Infrastructure.Interfaces;

namespace TranscriptService.Infrastructure.Services;

public class RabbitMqConsumer(
        IConnection connection,
        IChannel channel,
        ILogger<RabbitMqConsumer> logger) : IMessageConsumer, IAsyncDisposable
{
    private const string QueueName = "recordingchunk.completed";
    private string? _consumerTag;

    public event Func<ConsumedMessage, Task>? MessageReceived;

    public static async Task<RabbitMqConsumer> CreateAsync(
        IConfiguration config,
        ILogger<RabbitMqConsumer> logger)
    {
        var host = config["RabbitMQ:Host"] ?? throw new InvalidOperationException("Configuration value 'RabbitMQ:Host' is missing.");
        var port = int.Parse(config["RabbitMQ:Port"] ?? "5672");
        var prefetchCount = ushort.Parse(config["RabbitMQ:PrefetchCount"] ?? "1");

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = config["RabbitMQ:Username"]
                    ?? throw new InvalidOperationException("Configuration value 'RabbitMQ:Username' is missing."),
                Password = config["RabbitMQ:Password"]
                    ?? throw new InvalidOperationException("Configuration value 'RabbitMQ:Password' is missing.")
            };

            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Prefetch tuned independently of replica count - controls how many
            // unacked messages this consumer holds concurrently.
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: prefetchCount, global: false);

            logger.LogInformation(
                "Connected to RabbitMQ at {Host}:{Port}, consuming queue '{Queue}' (prefetch: {Prefetch})",
                host, port, QueueName, prefetchCount);

            return new RabbitMqConsumer(connection, channel, logger);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "Failed to connect to RabbitMQ at {Host}:{Port}. " +
                "Ensure RabbitMQ is running and configuration is correct",
                host, port);
            throw;
        }
    }

    public Task StartConsumingAsync(CancellationToken ct)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var messageType = nameof(ChunkCompletedMessage);

            try
            {
                logger.LogDebug("Received message from queue '{Queue}', delivery tag {Tag}",
                    QueueName, ea.DeliveryTag);

                var json = Encoding.UTF8.GetString(ea.Body.Span);
                var payload = JsonSerializer.Deserialize<ChunkCompletedMessage>(json)
                    ?? throw new InvalidOperationException($"Deserialized {messageType} was null.");

                // Hand off and return - this consumer's job stops here.
                // Whoever subscribed decides when (and whether) to ack/reject.
                if (MessageReceived is not null)
                {
                    await MessageReceived.Invoke(new ConsumedMessage(payload, ea.DeliveryTag));
                }
                else
                {
                    logger.LogWarning(
                        "No subscriber for {MessageType} on queue '{Queue}' - message will remain unacked until a subscriber processes it.",
                        messageType, QueueName);
                }
            }
            catch (Exception ex)
            {
                // Only deserialization/framework failures land here - this message
                // can never be processed, so reject it immediately without requeue.
                logger.LogError(ex,
                    "Failed to receive {MessageType} from queue '{Queue}', delivery tag {Tag}. Rejecting without requeue.",
                    messageType, QueueName, ea.DeliveryTag);

                await RejectAsync(ea.DeliveryTag, ct);
            }
        };

        return Task.Run(async () =>
        {
            _consumerTag = await channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct);

            logger.LogInformation("Started consuming queue '{Queue}' with tag {Tag}", QueueName, _consumerTag);
        }, ct);
    }

    /// <summary>
    /// Acks a message previously handed off via MessageReceived. Call this once
    /// processing (e.g. blob converted and uploaded) has fully completed - this
    /// is what gives at-least-once delivery semantics.
    /// </summary>
    public async Task AcknowledgeAsync(ulong deliveryTag, CancellationToken ct)
    {
        await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: ct);
        logger.LogDebug("Acked delivery tag {Tag} on queue '{Queue}'", deliveryTag, QueueName);
    }

    /// <summary>
    /// Nacks a message previously handed off via MessageReceived, without requeue,
    /// so a poison message doesn't loop forever. Route to a dead-letter exchange
    /// once one is configured on the queue.
    /// </summary>
    public async Task RejectAsync(ulong deliveryTag, CancellationToken ct)
    {
        await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: ct);
        logger.LogDebug("Nacked delivery tag {Tag} on queue '{Queue}' (no requeue)", deliveryTag, QueueName);
    }

    public async ValueTask DisposeAsync()
    {
        logger.LogInformation("Closing RabbitMQ consumer connection...");

        if (_consumerTag is not null)
        {
            await channel.BasicCancelAsync(_consumerTag);
        }

        await channel.CloseAsync();
        await connection.CloseAsync();

        logger.LogInformation("RabbitMQ consumer connection closed");
    }
}
