using System.Buffers;
using TranscriptService.Domain.ValueObjects;

namespace TranscriptService.Application.Interfaces;

public interface IWavConverter
{
    Task<Stream> ConvertAsync(RecordingId recordingId, ChunkId chunkId, IMemoryOwner<byte> audioBytes, CancellationToken cancellationToken);
}
