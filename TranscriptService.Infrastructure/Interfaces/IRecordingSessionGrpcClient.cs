using TranscriptService.Domain.ValueObjects;

namespace TranscriptService.Infrastructure.Interfaces;

public interface IRecordingSessionGrpcClient
{
    Task<RecordingMetadata> GetRecordingMetadataAsync(RecordingId recordingId, CancellationToken cancellationToken);
}
