using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TradeBull.Data;
using TradeBull.Data.Models;

namespace TradeBull.Api.Services
{
    public class StockPriceTickerOptions
    {
        public bool Enabled { get; set; }

        public required string MarketName { get; set; }

        public TimeSpan Interval { get; set; }
    }

    public class StockPriceTickerService : BackgroundService
    {
        private readonly ILogger<StockPriceTickerService> _logger;
        private readonly StockPriceTickerOptions _options;
        private readonly IServiceProvider _serviceProvider;

        public StockPriceTickerService(ILogger<StockPriceTickerService> logger, IOptions<StockPriceTickerOptions> options, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _options = options.Value;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
                return;

            _logger.LogInformation("Starting {ServiceName}...", nameof(StockPriceTickerService));

            using PeriodicTimer timer = new(_options.Interval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await UpdateStockPriceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException ocex)
            {
                _logger.LogWarning(ocex, "{ServiceName} is stopping due to a cancellation request.", nameof(StockPriceTickerService));
            }
        }

        private async Task UpdateStockPriceAsync(CancellationToken cancellationToken)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var stockContext = scope.ServiceProvider.GetRequiredService<StockContext>();

            var stocks = await stockContext.Stocks.Include(x => x.DayPerformance).Include(x => x.Market)
                .Where(x => x.Market != null && x.Market.Name == _options.MarketName)
                .ToListAsync(cancellationToken);

            var market = stocks.FirstOrDefault()?.Market;

            if (market == null)
                return;

            var currentTime = TimeOnly.FromDateTime(DateTime.UtcNow);

            if (currentTime < market.OpensAt || currentTime > market.ClosesAt)
            {
                _logger.LogWarning("Market is closed. Cannot update stock prices.");
                return;
            }

            foreach (var stock in stocks)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                if (stock == null)
                    continue;

                var lastPrice = stock.DayPerformance?.OrderByDescending(x => x.Date).FirstOrDefault()?.Price;

                if (lastPrice == null)
                {
                    // If we do not have any stock prices for today, let's use the last close price as base
                    lastPrice = (await stockContext.StockHistories.Where(x => x.StockId == stock.Id)
                        .OrderByDescending(x => x.Date)
                        .FirstOrDefaultAsync(cancellationToken))
                        ?.ClosePrice;

                    if (lastPrice == null)
                    {
                        _logger.LogWarning("Could not establish a base price for stock {StockName} to update", stock.TickerSymbol);
                        continue;
                    }
                }

                /*
                 * Let's randomly change the price a little by the order of mills / cents
                 * Price changes can be way milder, but we want to emulate drastic price changes
                */
                var priceChange = Convert.ToDecimal(Random.Shared.NextDouble() * 0.01d);
                var newPrice = lastPrice.Value + (Convert.ToBoolean(Random.Shared.Next(2)) ? -priceChange : priceChange);

                _logger.LogInformation("Updating {StockName} price from {PreviousStockPrice} to {StockPrice}", stock.TickerSymbol, lastPrice, newPrice);

                stockContext.StockDayPerformances.Add(new StockDayPerformance
                {
                    StockId = stock.Id,
                    Date = DateTime.UtcNow,
                    Price = newPrice
                });
                await stockContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}