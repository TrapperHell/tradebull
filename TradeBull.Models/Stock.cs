namespace TradeBull.Models
{
    public class Stock
    {
        public Guid Id { get; set; }

        public required string MarketName { get; set; }

        public required string EntityName { get; set; }

        public required string TickerSymbol { get; set; }

        public decimal? CurrentPrice { get; set; }
    }
}
