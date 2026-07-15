using TranscriptService.Application.Interfaces;
using TranscriptService.Domain.ValueObjects;
using TranscriptService.Infrastructure.Interfaces;

namespace TranscriptService.Application.Services;

public class RecordingSessionServices(
    IRecordingSessionGrpcClient grpcClient) : IRecordingSessionServices
{
    public async Task<RecordingMetadata> GetRecordingMetadataAsync(RecordingId recordingId, CancellationToken cancellationToken)
    {
        return await grpcClient.GetRecordingMetadataAsync(recordingId, cancellationToken);
    }
}
