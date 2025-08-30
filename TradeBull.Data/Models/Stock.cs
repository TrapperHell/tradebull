using System.ComponentModel.DataAnnotations;

namespace TradeBull.Data.Models
{
    public class Stock
    {
        public required Guid Id { get; set; }

        public required Guid MarketId { get; set; }

        public required string EntityName { get; set; }

        [MaxLength(5)]
        public required string TickerSymbol { get; set; }

        public Market? Market { get; set; }

        public ICollection<Trade>? Trades { get; set; }

        public ICollection<StockDayPerformance>? DayPerformance { get; set; }

        public ICollection<StockHistory>? History { get; set; }

        // Simple mapping without AutoMapper
        public virtual TradeBull.Models.Stock ToStockModel() => ToStockModel(this);

        public static TradeBull.Models.Stock ToStockModel(Stock stock)
        {
            ArgumentNullException.ThrowIfNull(stock);

            return new TradeBull.Models.Stock
            {
                Id = stock.Id,
                MarketName = stock.Market?.Name,
                EntityName = stock.EntityName,
                TickerSymbol = stock.TickerSymbol,
                CurrentPrice = stock.DayPerformance?.OrderByDescending(x => x.Date).FirstOrDefault()?.Price
            };
        }
    }
}