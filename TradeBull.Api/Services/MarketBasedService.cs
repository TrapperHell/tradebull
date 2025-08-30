using Microsoft.EntityFrameworkCore;
using TradeBull.Data;
using TradeBull.Data.Models;

namespace TradeBull.Api.Services
{
    public abstract class MarketBasedService : BackgroundService
    {
        public IServiceProvider ServiceProvider { get; init; }

        protected MarketBasedService(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        protected (AsyncServiceScope, StockContext) GetContext()
        {
            var scope = ServiceProvider.CreateAsyncScope();
            return (scope, scope.ServiceProvider.GetRequiredService<StockContext>());
        }

        protected async Task<Market> GetMarketAsync(string marketName, CancellationToken cancellationToken)
        {
            var (scope, context) = GetContext();

            var market = await context.Markets.FirstOrDefaultAsync(x => x.Name == marketName, cancellationToken);
            await scope.DisposeAsync();

            return market ?? throw new InvalidOperationException($"Unable to find a market with name: {marketName}");
        }
    }
}
