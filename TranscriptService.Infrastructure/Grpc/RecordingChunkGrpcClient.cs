using RecordingGrpcService.Grpc.Protos;
using TranscriptService.Domain.Entities;
using TranscriptService.Domain.ValueObjects;
using TranscriptService.Infrastructure.Interfaces;
using TranscriptService.Infrastructure.Mappers;

namespace TranscriptService.Infrastructure.Grpc;

public class RecordingChunkGrpcClient(
    RecordingChunkService.RecordingChunkServiceClient grpcClient,
    RecordingChunkMapper mapper) : IRecordingChunkGrpcClient
{
    public async Task<RecordingChunk> GetRecordingChunkAsync(ChunkId chunkId, CancellationToken cancellationToken)
    {
        var response = await grpcClient.GetRecordingChunkAsync(
            new GetRecordingChunkRequest { ChunkId = mapper.MapChunkId(chunkId) },
            cancellationToken: cancellationToken);
        return mapper.ToDomain(response.RecordingChunk);
    }
    public async Task<List<RecordingChunk>> GetSubsequentRecordingChunksAsync(ChunkId chunkId,
        CancellationToken cancellationToken)
    {
        var response = await grpcClient.GetSubsequentRecordingChunksAsync(
            new GetSubsequentRecordingChunksRequest { ChunkId = mapper.MapChunkId(chunkId) },
            cancellationToken: cancellationToken);
        return mapper.ToDomainList(response.RecordingChunks);
    }

    public async Task<bool> UpdateWavPathAsync(ChunkId chunkId, string wavStoragePath, CancellationToken cancellationToken)
    {
        var response = await grpcClient.UpdateRecordingChunkWavAsync(
            new UpdateRecordingChunkWavRequest { ChunkId = mapper.MapChunkId(chunkId), WavPath = wavStoragePath },
            cancellationToken: cancellationToken
            );
        return response.IsSuccess;
    }
}
