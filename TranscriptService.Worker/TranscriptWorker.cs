using TranscriptService.Application.Interfaces;
using TranscriptService.Domain.ValueObjects;
using TranscriptService.Infrastructure.Interfaces;

namespace TranscriptService.Worker
{
    public class TranscriptWorker(        
        IMessageConsumer messageConsumer,
        IMessagePublisher messagePublisher,
        IServiceScopeFactory scopeFactory,
        ILogger<TranscriptWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {            
            messageConsumer.MessageReceived += async consumed =>
            {
                try
                {   
                    await ProcessAsync(consumed, stoppingToken);
                    await messageConsumer.AcknowledgeAsync(consumed.DeliveryTag, stoppingToken);
                }
                catch
                {
                    await messageConsumer.RejectAsync(consumed.DeliveryTag, stoppingToken);
                    logger.LogError("Failed to process message with delivery tag {DeliveryTag}", consumed.DeliveryTag);
                }
                //await Task.Delay(3000, stoppingToken); // Simulate some delay
            };
            await messageConsumer.StartConsumingAsync(stoppingToken);
        }

        private async Task ProcessAsync(ConsumedMessage consumedMessage, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();

            var chunkProcessor = scope.ServiceProvider.GetRequiredService<IChunkProcessor>();
            var wavConverter = scope.ServiceProvider.GetRequiredService<IWavConverter>();
            var audioObjectStorage = scope.ServiceProvider.GetRequiredService<IAudioObjectStorage>();
            var recordingChunkServices = scope.ServiceProvider.GetRequiredService<IRecordingChunkServices>();

            var chunkId = ChunkId.Of(consumedMessage.Payload.ChunkId);
            using var audioData = await chunkProcessor.ProcessChunkAsync(chunkId, cancellationToken);
            //ReadOnlyMemory<byte> data = audioData.Memory;

            var wavStream = await wavConverter.ConvertAsync(
                RecordingId.Of(consumedMessage.Payload.RecordingId),
                chunkId, audioData, cancellationToken);

            var destinationPath = $"converts/{DateTime.UtcNow:yyyy-MM-dd}/{chunkId}.wav";
            var result = await audioObjectStorage.UploadChunkAsync(destinationPath, wavStream, cancellationToken);

            if (result)
            {
                logger.LogInformation("WAV file uploaded for ChunkId:{ChunkId}", chunkId);

                await recordingChunkServices.UpdateWavPathAsync(chunkId, destinationPath, cancellationToken);

                await messagePublisher.PublishAsync<ChunkWavConvertCompletedMessage>(
                    new ChunkWavConvertCompletedMessage(
                        ChunkId: chunkId.Value,
                        RecordingId: consumedMessage.Payload.RecordingId,
                        CompletedAt: DateTimeOffset.UtcNow), cancellationToken);
            }
            else
            {
                logger.LogInformation("WAV file upload failed for ChunkId:{ChunkId}", chunkId);
                throw new InvalidOperationException("Upload failed:");
            }
        }
    }
}
