using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TradeBull.Messaging.Options
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IValidateOptions<Connection>, ConnectionValidation>();
            services.AddOptions<Connection>().Bind(configuration.GetSection("Messaging")).ValidateDataAnnotations().ValidateOnStart();
            services.AddSingleton<IQueueMessaging, QueueMessaging>();

            return services;
        }
    }
}
