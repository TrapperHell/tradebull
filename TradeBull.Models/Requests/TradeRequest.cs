namespace TradeBull.Models.Requests
{
    public class TradeRequest
    {
        public required TradeType Type { get; set; }

        public required TradeCondition Condition { get; set; }

        public required int Quantity { get; set; }

        public decimal? RequestedSharePrice { get; set; }
    }
}
