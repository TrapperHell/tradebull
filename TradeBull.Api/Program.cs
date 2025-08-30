using System.Text.Json.Serialization;
using Asp.Versioning;
using FluentValidation;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using TradeBull.Data;
using TradeBull.Api.Services;
using TradeBull.Messaging.Options;
using TradeBull.Messaging;

namespace TradeBull.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            var apiVersioningBuilder = builder.Services.AddApiVersioning(x =>
            {
                x.ReportApiVersions = true;

                x.DefaultApiVersion = new ApiVersion(1);
                x.AssumeDefaultVersionWhenUnspecified = true;

                // Add support for versioning based on custom header
                x.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("X-API-VERSION"));
            });

            apiVersioningBuilder.AddApiExplorer(x =>
            {
                x.GroupNameFormat = "'v'V";
                x.SubstituteApiVersionInUrl = true;
            });

            builder.Services.AddDatabaseAccess(builder.Configuration);
            builder.Services.AddMessaging(builder.Configuration);
            builder.Services.AddValidatorsFromAssemblyContaining<Validation.TradeRequestValidator>();
            builder.Services.AddFluentValidationAutoValidation();

            builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.Configure<StockPriceTickerOptions>(builder.Configuration.GetSection($"Services:{nameof(StockPriceTickerService)}"));
            builder.Services.AddHostedService<StockPriceTickerService>();
            builder.Services.Configure<StockHistoryMaintenanceOptions>(builder.Configuration.GetSection($"Services:{nameof(StockHistoryMaintenanceService)}"));
            builder.Services.AddHostedService<StockHistoryMaintenanceService>();
            // TODO: Create a service to cancel old trades that haven't been finalized yet

            var app = builder.Build();

            // HACK: This should be Development only, but for the purposes of the demo we want the DB and Swagger to be available
            if (app.Environment.IsDevelopment() || true)
            {
                app.UseSwagger();
                app.UseSwaggerUI();

                using var scope = app.Services.CreateScope();
                using var stockContext = scope.ServiceProvider.GetRequiredService<StockContext>();
                await stockContext.Database.EnsureCreatedAsync();
            }

            app.MapControllers();

            var queueMessaging = app.Services.GetRequiredService<IQueueMessaging>();
            await queueMessaging.InitializeAsync($"{nameof(TradeBull)}-API");
            await app.RunAsync();
        }
    }
}
