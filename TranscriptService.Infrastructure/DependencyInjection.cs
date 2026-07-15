using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using RecordingGrpcService.Grpc.Protos;
using TranscriptService.Infrastructure.Grpc;
using TranscriptService.Infrastructure.Interfaces;
using TranscriptService.Infrastructure.Mappers;
using TranscriptService.Infrastructure.Services;

namespace TranscriptService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)

    {
        #region RabbitMq

        services.AddSingleton<IMessageConsumer>(sp =>
            RabbitMqConsumer.CreateAsync(
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<RabbitMqConsumer>>()
            ).GetAwaiter().GetResult());

        services.AddSingleton<IMessagePublisher>(sp =>
            RabbitMqPublisher.CreateAsync(
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<RabbitMqPublisher>>()
            ).GetAwaiter().GetResult());

        #endregion

        services.AddSingleton<RecyclableMemoryStreamManager>(_ =>
            new RecyclableMemoryStreamManager(new RecyclableMemoryStreamManager.Options
            {                
                BlockSize = 1024 * 1024,          // 1MB pooled blocks
                LargeBufferMultiple = 1024 * 1024,
                MaximumBufferSize = 5 * 1024 * 1024,
                AggressiveBufferReturn = true
            }));

        #region "Grpc"

        services.AddGrpcClient<RecordingChunkService.RecordingChunkServiceClient>(o =>
        {
            o.Address = new Uri(configuration["RecordingGrpcService:Address"]!);
        });

        services.AddGrpcClient<RecordingService.RecordingServiceClient>(o =>
        {
            o.Address = new Uri(configuration["RecordingGrpcService:Address"]!);
        });

        services.AddSingleton<RecordingChunkMapper>();
        services.AddSingleton<RecordingMapper>();

        services.AddScoped<IRecordingChunkGrpcClient, RecordingChunkGrpcClient>();
        services.AddScoped<IRecordingSessionGrpcClient, RecordingSessionGrpcClient>();

        #endregion

        #region "Blob"

        services.Configure<AzureBlobStorageOptions>(
            configuration.GetSection(AzureBlobStorageOptions.SectionName));
        services.AddSingleton<BlobServiceClient>(sp =>
            new BlobServiceClient(configuration["Azure:StorageConnectionString"]));
        services.AddScoped<IAudioObjectStorage, AzureAudioObjectStorage>();

        #endregion

        return services;
    }
}
