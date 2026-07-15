using TranscriptService.Domain.ValueObjects;

namespace TranscriptService.Infrastructure.Interfaces;

public interface IMessageConsumer
{
    event Func<ConsumedMessage, Task>? MessageReceived;
    Task StartConsumingAsync(CancellationToken ct);
    Task AcknowledgeAsync(ulong deliveryTag, CancellationToken ct);
    Task RejectAsync(ulong deliveryTag, CancellationToken ct);
}
