using Serilog;
using TranscriptService.Application;
using TranscriptService.Infrastructure;
using TranscriptService.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAppService(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

#region Serilog
builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});
#endregion

builder.Services.AddHostedService<TranscriptWorker>();

var host = builder.Build();
host.Run();
