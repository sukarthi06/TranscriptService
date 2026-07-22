using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using TranscriptService.Application.Interfaces;
using TranscriptService.Application.Services;

namespace TranscriptService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAppService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<RecyclableMemoryStreamManager>();

        services.AddScoped<IRecordingChunkServices, RecordingChunkServices>();
        services.AddScoped<IRecordingSessionServices, RecordingSessionServices>();
        services.AddScoped<IChunkProcessor, ChunkProcessor>();
        services.AddScoped<IWavConverter, WavConverter>();

        return services;
    }
}
