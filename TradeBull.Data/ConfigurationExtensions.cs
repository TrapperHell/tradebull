using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TradeBull.Data
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddDatabaseAccess(this IServiceCollection services, IConfiguration configuration)
            => services.AddDbContextFactory<StockContext>(options => options.UseSqlite(configuration.GetConnectionString("TradeDb")));
    }
}
