using TranscriptService.Domain.Entities;
using TranscriptService.Domain.ValueObjects;

namespace TranscriptService.Infrastructure.Interfaces;

public interface IRecordingChunkGrpcClient
{
    Task<RecordingChunk> GetRecordingChunkAsync(ChunkId chunkId, CancellationToken cancellationToken);
    Task<List<RecordingChunk>> GetSubsequentRecordingChunksAsync(ChunkId chunkId, CancellationToken cancellationToken);
    Task<bool> UpdateWavPathAsync(ChunkId chunkId, string wavStoragePath, CancellationToken cancellationToken);
}
