using TranscriptService.Domain.Entities;
using TranscriptService.Domain.ValueObjects;

namespace TranscriptService.Application.Interfaces;

public interface IRecordingChunkServices
{
    Task<RecordingChunk> GetRecordingChunkAsync(Guid chunkId, CancellationToken cancellationToken);
    Task<List<RecordingChunk>> GetSubsequentRecordingChunksAsync(ChunkId chunkId, CancellationToken cancellationToken);
    Task<bool> UpdateWavPathAsync(ChunkId chunkId, string wavStoragePath, CancellationToken cancellationToken);
}
