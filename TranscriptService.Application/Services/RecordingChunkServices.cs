using Microsoft.Extensions.Logging;
using TranscriptService.Application.Interfaces;
using TranscriptService.Domain.Entities;
using TranscriptService.Domain.ValueObjects;
using TranscriptService.Infrastructure.Interfaces;

namespace TranscriptService.Application.Services;

public class RecordingChunkServices(
    IRecordingChunkGrpcClient grpcClient,
    ILogger<RecordingChunkServices> logger) : IRecordingChunkServices
{
    public async Task<RecordingChunk> GetRecordingChunkAsync(Guid chunkId, CancellationToken cancellationToken)
    {
        return await grpcClient.GetRecordingChunkAsync(ChunkId.Of(chunkId), cancellationToken);
    }

    public async Task<List<RecordingChunk>> GetSubsequentRecordingChunksAsync(ChunkId chunkId,
        CancellationToken cancellationToken)
    {
        return await grpcClient.GetSubsequentRecordingChunksAsync(chunkId, cancellationToken);
    }

    public async Task<bool> UpdateWavPathAsync(ChunkId chunkId, string wavStoragePath, CancellationToken cancellationToken)
    {
        return await grpcClient.UpdateWavPathAsync(chunkId, wavStoragePath, cancellationToken);
    }
}
