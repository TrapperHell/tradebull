using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeBull.Data;
using TradeBull.Messaging;
using TradeBull.Models;

namespace TradeBull.Processor
{
    public class WorkerService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IQueueMessaging _queueMessaging;
        private readonly ILogger<WorkerService> _logger;

        public WorkerService(IServiceProvider serviceProvider, IQueueMessaging queueMessaging, ILogger<WorkerService> logger)
        {
            _serviceProvider = serviceProvider;
            _queueMessaging = queueMessaging;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _queueMessaging.ListenForMessagesAsync<Data.Models.Trade>(async x =>
            {
                try
                {
                    await ProcessTradeAsync(x, cancellationToken);
                }
                catch (Exception ex)
                {
                    // HACK: By swallowing the exception, we are preventing the message from being NACK'd
                    _logger.LogError(ex, "Unable to process message");
                }
            });
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _queueMessaging.DisposeAsync();
        }

        public async Task ProcessTradeAsync(Data.Models.Trade trade, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received Trade request {@Trade}", trade);

            await using var scope = _serviceProvider.CreateAsyncScope();
            using var context = scope.ServiceProvider.GetRequiredService<StockContext>();

            await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var currentTrade = await context.Trades.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == trade.Id, cancellationToken);

            if (currentTrade == null)
            {
                _logger.LogWarning("Unable to find a corresponding trade in the DB with ID {TradeId}", trade.Id);
                return;
            }

            // Could have used a ternary operator
            var desiredTradeType = (TradeType)Math.Abs((int)trade.Type - 1);

            var market = await context.Markets.FirstOrDefaultAsync(x => string.Equals(x.Name, Constants.DefaultMarketName), cancellationToken);

            if (market == null)
            {
                _logger.LogWarning("Unable to find market '{MarketName}'", Constants.DefaultMarketName);
                return;
            }

            var currentTime = TimeOnly.FromDateTime(DateTime.UtcNow);

            if (currentTime < market.OpensAt || currentTime > market.ClosesAt)
            {
                _logger.LogDebug("Market is closed, cannot process trade at this time");
                return;
            }

            // TODO: We need to verify that the user that initiated the trade has suffficient balance to complete it
            var pendingTrade = await context.Trades
            .Include(x => x.User)
                .Where(x => x.StockId == trade.StockId &&
                x.Status == TradeStatus.Pending &&
                x.UserId != trade.UserId &&
                x.Type == desiredTradeType &&
                // Partial fills are not currently supported
                x.Quantity == trade.Quantity)
            .OrderBy(x => x.RegisteredAt)
            .FirstOrDefaultAsync(cancellationToken);

            if (pendingTrade == null)
            {
                _logger.LogWarning("No matching pending trades have been found for Trade Id {TradeId}", trade.Id);
                return;
            }

            var stockPrice = (await context.StockDayPerformances
                .Where(x => x.StockId == trade.StockId)
                .OrderByDescending(x => x.Date)
                .FirstOrDefaultAsync(cancellationToken))
                ?.Price;

            if (stockPrice == null)
            {
                _logger.LogWarning("Unable to find the latest stock price for Stock Id {StockId}", trade.StockId);
                return;
            }

            currentTrade.SharePrice = pendingTrade.SharePrice = stockPrice;

            if (ProcessTrade(ref currentTrade, Constants.TradeFlatFee) && ProcessTrade(ref pendingTrade, Constants.TradeFlatFee))
            {
                await context.SaveChangesAsync(cancellationToken);
                await context.Database.CommitTransactionAsync(cancellationToken);
            }
            else
                await context.Database.RollbackTransactionAsync(cancellationToken);
        }

        public bool ProcessTrade(ref Data.Models.Trade trade, decimal fees)
        {
            ArgumentNullException.ThrowIfNull(trade);
            ArgumentNullException.ThrowIfNull(trade.SharePrice);
            ArgumentNullException.ThrowIfNull(trade.User);

            var totalPrice = (trade.SharePrice.Value * trade.Quantity) + (trade.Type == TradeType.Buy ? fees : -fees);

            if (trade.Type == TradeType.Buy && trade.User.AccountBalance < totalPrice)
            {
                _logger.LogWarning("User {UserId} has insufficient balance to perform trade with Id {TradeId}", trade.UserId, trade.Id);
                return false;
            }

            if (trade.Type == TradeType.Buy)
                totalPrice = -totalPrice;

            trade.ProcessedAt = DateTime.UtcNow;
            trade.Status = TradeStatus.Completed;
            trade.TotalPrice = totalPrice;
            trade.User.AccountBalance += totalPrice;
            return true;
        }
    }
}
