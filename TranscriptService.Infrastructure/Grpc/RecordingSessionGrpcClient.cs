using RecordingGrpcService.Grpc.Protos;
using TranscriptService.Domain.ValueObjects;
using TranscriptService.Infrastructure.Interfaces;
using TranscriptService.Infrastructure.Mappers;

namespace TranscriptService.Infrastructure.Grpc;

public class RecordingSessionGrpcClient(
    RecordingService.RecordingServiceClient grpcClient,
    RecordingMapper mapper) : IRecordingSessionGrpcClient
{
    public async Task<RecordingMetadata> GetRecordingMetadataAsync(RecordingId recordingId, CancellationToken cancellationToken)
    {
        var response = await grpcClient.GetRecordingMetadataAsync(
            new GetRecordingMetadataRequest { RecordingId = mapper.MapRecordingId(recordingId) },
            cancellationToken: cancellationToken);
        return mapper.ToDomain(response.Metadata);
    }
}
