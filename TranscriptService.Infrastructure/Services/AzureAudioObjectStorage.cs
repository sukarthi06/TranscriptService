using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TranscriptService.Infrastructure.Interfaces;

namespace TranscriptService.Infrastructure.Services;

public sealed class AzureAudioObjectStorage(
        BlobServiceClient blobServiceClient,
        IOptions<AzureBlobStorageOptions> options,
        ILogger<AzureAudioObjectStorage> logger) : IAudioObjectStorage
{
    private readonly AzureBlobStorageOptions _options = options.Value;
    public async Task<long> GetLengthAsync(string path, CancellationToken ct)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(_options.SourceContainer)
            .GetBlobClient(path);
        if (blobClient is null || !blobClient.Exists(ct))
        {
            logger.LogInformation("Could not find Azure blob storage path: {@Path}", path);
            return 0;
        }
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: ct);
        return properties.Value.ContentLength;
    }
    public async Task<Stream> OpenReadStreamAsync(string path, CancellationToken ct)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(_options.SourceContainer)
            .GetBlobClient(path);
        
        // OpenReadAsync streams lazily from the blob rather than downloading
        // it into memory upfront - required given source sizes.
        return await blobClient.OpenReadAsync(cancellationToken: ct);
    }
    public async Task<Stream> ReadRangeStreamAsync(string path, long length, long size, CancellationToken ct)
    {
        long start = Math.Max(0, length - size);
        long len = length - start;

        var options = new BlobDownloadOptions
        {
            Range = new HttpRange(offset: start, length: len)
        };

        var blobClient = blobServiceClient
            .GetBlobContainerClient(_options.SourceContainer)
            .GetBlobClient(path);

        BlobDownloadStreamingResult result = await blobClient.DownloadStreamingAsync(options);
        return result.Content;
    }

    public async Task<bool> UploadChunkAsync(string path, Stream content, CancellationToken ct)
    {
        //var containerClient = blobServiceClient.GetBlobContainerClient(_options.WavContainer);
        //await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);
        //var blobClient = containerClient.GetBlobClient(path);
        var blobClient = blobServiceClient
            .GetBlobContainerClient(_options.SourceContainer)
            .GetBlobClient(path);

        await blobClient.UploadAsync(
                content,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = "audio/wav" }
                },
                ct);
        return true;
    }
}
