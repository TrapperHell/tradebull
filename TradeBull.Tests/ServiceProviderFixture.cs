using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TradeBull.Data;

namespace TradeBull.Tests
{
    public class ServiceProviderFixture
    {
        public IServiceProvider ServiceProvider { get; init; }

        public ServiceProviderFixture()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddDbContext<StockContext>(x => x.UseInMemoryDatabase(databaseName: "TradeDb"));

            serviceCollection.AddLogging(builder =>
            {
                builder.AddProvider(NullLoggerProvider.Instance);
            });

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }
    }
}
