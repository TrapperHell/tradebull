namespace TradeBull.Models
{
    public class Trade
    {
        public TradeType Type { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public int Quantity { get; set; }

        public decimal? SharePrice { get; set; }
    }
}
