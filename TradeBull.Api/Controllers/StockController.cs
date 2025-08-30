using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeBull.Data;
using TradeBull.Messaging;
using TradeBull.Models;
using TradeBull.Models.Requests;

namespace TradeBull.Api.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class StockController : ControllerBase
    {
        // Note: The `alpha` constraints assumes that all stock symbols are A-Z only, which may not always be the case
        [HttpGet("{tickerSymbol:alpha}/trades")]
        public async Task<IActionResult> GetTradeAsync([FromServices] StockContext stockContext, [FromRoute] string tickerSymbol, CancellationToken cancellationToken = default)
        {
            // TODO: Move to business-logic layer
            return Ok(await stockContext.Trades.Include(x => x.Stock)
                .Where(x => x.Stock != null &&
                x.Stock.TickerSymbol.Equals(tickerSymbol.ToUpperInvariant()) &&
                x.Status == TradeStatus.Completed)
                .Select(x => x.ToTradeModel())
                .ToListAsync(cancellationToken));
        }

        [HttpGet("{tickerSymbol:alpha}/my-trades")]
        public async Task<IActionResult> GetMyTradesAsync([FromServices] StockContext stockContext, [FromRoute] string tickerSymbol, [FromQuery] TradeStatus? status = null, CancellationToken cancellationToken = default)
        {
            // TODO: Move to business-logic layer
            return Ok(await stockContext.Trades.Include(x => x.Stock)
                .Where(x => x.Stock != null &&
                x.Stock.TickerSymbol.Equals(tickerSymbol.ToUpperInvariant()) &&
                (status == null || x.Status == status) &&
                // HACK: UserId should be derived from the authenticated user
                x.UserId == Constants.DefaultUserId)
                .Select(x => x.ToTradeModel<SensitiveTrade>())
                .ToListAsync(cancellationToken));
        }

        [HttpPost("{tickerSymbol:alpha}/trade")]
        public async Task<IActionResult> TradeAsync([FromServices] IQueueMessaging queueMessaging, [FromServices] StockContext stockContext, [FromRoute] string tickerSymbol, [FromBody] TradeRequest request, CancellationToken cancellationToken = default)
        {
            // TODO: Move to business-logic layer
            var stock = await stockContext.Stocks.FirstOrDefaultAsync(x => x.TickerSymbol.Equals(tickerSymbol.ToUpperInvariant()), cancellationToken);

            if (stock == null)
                return BadRequest(new { Message = $"Provided {nameof(tickerSymbol)} does not exist." });

            var tradeId = Guid.NewGuid();

            var trade = new Data.Models.Trade
            {
                Id = tradeId,
                // HACK: UserId should be derived from the authenticated user
                UserId = Constants.DefaultUserId,
                StockId = stock.Id,
                RegisteredAt = DateTime.UtcNow,
                Type = request.Type,
                Condition = request.Condition,
                Quantity = request.Quantity,
                RequestedSharePrice = request.RequestedSharePrice
            };

            await queueMessaging.PublishMessageAsync(trade);

            /*
             * Use a sort of "Outbox pattern", except that the message is first saved to database,
             * and in this case, I'm not breaking this down into another BackgroundService that
             * picks items from the DB and publishes them in turn.
            */
            stockContext.Trades.Add(trade);
            await stockContext.SaveChangesAsync(cancellationToken);

            return Ok(new { Id = tradeId });
        }
    }
}
