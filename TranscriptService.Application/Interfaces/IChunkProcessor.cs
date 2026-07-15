using System.Buffers;
using TranscriptService.Domain.ValueObjects;

namespace TranscriptService.Application.Interfaces;

public interface IChunkProcessor
{
    Task<IMemoryOwner<byte>> ProcessChunkAsync(ChunkId chunkId, CancellationToken cancellationToken);
}
