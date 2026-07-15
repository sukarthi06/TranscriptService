namespace TranscriptService.Domain.ValueObjects;

public record ChunkCompletedMessage(
    Guid ChunkId,
    Guid RecordingId,
    DateTime CompletedAt);
