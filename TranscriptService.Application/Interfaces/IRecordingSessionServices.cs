using TranscriptService.Domain.ValueObjects;

namespace TranscriptService.Application.Interfaces;

public interface IRecordingSessionServices
{
    Task<RecordingMetadata> GetRecordingMetadataAsync(RecordingId recordingId, CancellationToken cancellationToken);
}
