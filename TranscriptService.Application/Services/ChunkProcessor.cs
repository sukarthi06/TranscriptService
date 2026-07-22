using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System.Buffers;
using TranscriptService.Application.Interfaces;
using TranscriptService.Domain.ValueObjects;
using TranscriptService.Infrastructure.Common;
using TranscriptService.Infrastructure.Interfaces;

namespace TranscriptService.Application.Services;

public class ChunkProcessor(
    IRecordingChunkServices recordingChunkService,
    IAudioObjectStorage storage,
    RecyclableMemoryStreamManager streamManager,
    ILogger<ChunkProcessor> logger) : IChunkProcessor
{
    private const int OverlapSizeBytes = 512 * 1024;

    public async Task<IMemoryOwner<byte>> ProcessChunkAsync(ChunkId chunkId, CancellationToken ct)
    {
        string storagePath = string.Empty;
        string overlapPath = string.Empty;

        var chunks = await recordingChunkService.GetSubsequentRecordingChunksAsync(chunkId, ct);

        if (chunks.Count == 0)
        {
            logger.LogError("No chunks found for chunkId {ChunkId}.", chunkId);
            throw new InvalidOperationException($"No chunks found for chunkId {chunkId}.");
        }

        bool hasOverlap = chunks.Count == 2;

        if (!hasOverlap)
        {
            storagePath = chunks[0].StoragePath;
        }
        else
        {
            overlapPath = chunks[0].StoragePath;
            storagePath = chunks[1].StoragePath;
        }

        byte[]? storageBytes = null;
        byte[]? bufferOverlap = null;

        try
        {
            // --- Read main chunk to EOF (size not known up front) ---
            int storageBytesRead;
            try
            {
                await using var storageReader = await storage.OpenReadStreamAsync(storagePath, ct);
                await using var readStream = streamManager.GetStream("chunk-read");

                // Grows via pooled segments internally - no fixed ceiling, reads until true EOF.
                await storageReader.CopyToAsync(readStream, ct);
                storageBytesRead = (int)readStream.Length;

                // Copy out into a right-sized pooled array now that we know the real length,
                // since callers expect a contiguous pooled buffer via PooledArrayMemoryOwner.
                storageBytes = ArrayPool<byte>.Shared.Rent(storageBytesRead);
                readStream.Position = 0; // must reset before reading back out

                int offset = 0;
                int bytesRead;
                while (offset < storageBytesRead &&
                       (bytesRead = await readStream.ReadAsync(storageBytes.AsMemory(offset, storageBytesRead - offset), ct)) > 0)
                {
                    offset += bytesRead;
                }

                logger.LogDebug("Fetched {BytesRead} bytes for chunkId {ChunkId} from storage path {StoragePath}.",
                    storageBytesRead, chunkId, storagePath);
            }
            catch
            {
                logger.LogError("While fetching storage {StoragePath} an error occurred.", storagePath);
                throw;
            }

            if (!hasOverlap)
            {
                // Caller owns this - it will Dispose() to return storageBytes to the pool
                var owner = new PooledArrayMemoryOwner(storageBytes, storageBytesRead);
                storageBytes = null!; // prevent our finally block from also returning it
                return owner;
            }

            // --- Read overlap (last OverlapSizeBytes of the previous chunk) ---
            bufferOverlap = ArrayPool<byte>.Shared.Rent(OverlapSizeBytes);
            int overlapBytesRead;

            long overlapStorageLength = await storage.GetLengthAsync(overlapPath, ct);
            long rangeOffset = Math.Max(0, overlapStorageLength - OverlapSizeBytes);
            long rangeLength = overlapStorageLength - rangeOffset;

            await using (var overlapReader = await storage.ReadRangeStreamAsync(overlapPath, rangeOffset, rangeLength, ct))
            {
                int offset = 0;
                int bytesRead;
                while (offset < rangeLength &&
                       (bytesRead = await overlapReader.ReadAsync(bufferOverlap.AsMemory(offset, (int)rangeLength - offset), ct)) > 0)
                {
                    offset += bytesRead;
                }
                overlapBytesRead = offset;
            }

            logger.LogDebug("Fetched {BytesRead} bytes from overlap path {OverlapPath}.", overlapBytesRead, overlapPath);

            // --- Combine overlap + storage into the final owned buffer ---
            int totalLength = overlapBytesRead + storageBytesRead;
            var combined = ArrayPool<byte>.Shared.Rent(totalLength);

            Buffer.BlockCopy(bufferOverlap, 0, combined, 0, overlapBytesRead);
            Buffer.BlockCopy(storageBytes, 0, combined, overlapBytesRead, storageBytesRead);

            return new PooledArrayMemoryOwner(combined, totalLength);
        }
        finally
        {
            // storageBytes is nulled out above if ownership was transferred to the caller
            if (storageBytes != null) ArrayPool<byte>.Shared.Return(storageBytes);
            if (bufferOverlap != null) ArrayPool<byte>.Shared.Return(bufferOverlap);
        }
    }
}