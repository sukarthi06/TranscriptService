namespace TranscriptService.Domain.ValueObjects;

/// <summary>
/// A message handed off from the queue, paired with the delivery tag needed
/// to acknowledge or reject it later.
/// </summary>
public record ConsumedMessage(ChunkCompletedMessage Payload, ulong DeliveryTag);
