using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TradeBull.Data;
using TradeBull.Messaging;
using TradeBull.Messaging.Options;

namespace TradeBull.Processor
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddLogging(x =>
            {
                x.ClearProviders();
                x.AddSerilog();
            });
            builder.Services.AddDatabaseAccess(builder.Configuration);
            builder.Services.AddMessaging(builder.Configuration);
            builder.Services.AddHostedService<WorkerService>();

            var host = builder.Build();

            var queueMessaging = host.Services.GetRequiredService<IQueueMessaging>();
            await queueMessaging.InitializeAsync($"{nameof(TradeBull)}-Processor");

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            Log.Logger.Information("Starting processor...");

            await host.RunAsync();
        }
    }
}
