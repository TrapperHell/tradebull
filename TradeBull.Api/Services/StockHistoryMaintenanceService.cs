using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TradeBull.Data.Models;
using TradeBull.Models;

namespace TradeBull.Api.Services
{
    public class StockHistoryMaintenanceOptions
    {
        public bool Enabled { get; set; }

        public required string MarketName { get; set; }

        public TimeSpan? MarketCloseDelay { get; set; }
    }

    public class StockHistoryMaintenanceService : MarketBasedService
    {
        private readonly ILogger<StockHistoryMaintenanceService> _logger;
        private readonly StockHistoryMaintenanceOptions _options;

        public StockHistoryMaintenanceService(ILogger<StockHistoryMaintenanceService> logger, IOptions<StockHistoryMaintenanceOptions> options, IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
                return;

            _logger.LogInformation("Starting {ServiceName}...", nameof(StockHistoryMaintenanceService));

            var marketCloseTime = (await GetMarketAsync(_options.MarketName, stoppingToken))?.ClosesAt ?? TimeOnly.MinValue;
            marketCloseTime = marketCloseTime.Add(_options.MarketCloseDelay ?? TimeSpan.Zero);

            var nextCloseTime = DateTime.UtcNow.Date.Add(marketCloseTime.ToTimeSpan());

            if (nextCloseTime < DateTime.UtcNow)
                nextCloseTime = nextCloseTime.AddDays(1);

            _logger.LogInformation("Waiting until {TargetTime} to perform stock history maintenance...", nextCloseTime);
            await Task.Delay(nextCloseTime.Subtract(DateTime.UtcNow), stoppingToken);

            // Perform the initial maintenance
            await UpdateStockHistoryAsync(stoppingToken);

            using PeriodicTimer timer = new(TimeSpan.FromDays(1));

            try
            {
                /*
                 * HACK: This approach is very much susceptible to "clock drift" issues, since this waits
                 * for the processing to be done before scheduling another run. This means that slowly
                 * but surely the clock will run way later than intended.
                 * 
                 * In an ideal world, such maintenance tasks should be performed by separate functions
                 * that can be triggered through scheduled tasks / CRONs.
                */
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await UpdateStockHistoryAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException ocex)
            {
                _logger.LogWarning(ocex, "{ServiceName} is stopping due to a cancellation request.", nameof(StockHistoryMaintenanceService));
            }
        }

        private async Task UpdateStockHistoryAsync(CancellationToken cancellationToken)
        {
            var (scope, context) = GetContext();

            /*
             * In addition to using a Transaction, we're using Serializable IsolationLevel which is very
             * aggressive, but it should give us the consistency we need to do the consolidation.
            */
            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

            var stockPerformanceGroups = context.StockDayPerformances.GroupBy(x => x.Stock);

            foreach (var stockPerformance in stockPerformanceGroups)
            {
                var performanceEntries = stockPerformance?.GroupBy(x => x.Date.Date).SelectMany(x => x).ToList();

                if (stockPerformance?.Key == null || (performanceEntries?.Count ?? 0) == 0)
                    continue;

                var targetDate = performanceEntries!.First().Date.Date;

                var stockTrades = await context.Trades.Where(x => x.StockId == stockPerformance.Key.Id &&
                    x.Status == TradeStatus.Completed &&
                    x.ProcessedAt.HasValue && x.ProcessedAt.Value.Date == targetDate)
                    .SumAsync(x => x.Quantity, cancellationToken);

                context.StockHistories.Add(new StockHistory
                {
                    StockId = stockPerformance.Key.Id,
                    Date = DateOnly.FromDateTime(targetDate),
                    OpenPrice = performanceEntries.First().Price,
                    ClosePrice = performanceEntries.Last().Price,
                    LowestPrice = performanceEntries.Min(x => x.Price),
                    HighestPrice = performanceEntries.Max(x => x.Price),
                    TradeVolume = stockTrades
                });

                await context.SaveChangesAsync(cancellationToken);

                await context.StockDayPerformances
                    .Where(x => x.StockId == stockPerformance.Key.Id && x.Date.Date == targetDate)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            await scope.DisposeAsync();
        }
    }
}
