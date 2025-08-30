namespace TradeBull.Models
{
    public class SensitiveTrade : Trade
    {
        public Guid Id { get; set; }

        public TradeCondition Condition { get; set; }

        public DateTime? RegisteredAt { get; set; }

        public decimal? RequestedSharePrice { get; set; }

        public TradeStatus Status { get; set; }

        public decimal? TotalPrice { get; set; }
    }
}
