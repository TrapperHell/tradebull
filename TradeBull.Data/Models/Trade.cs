using TradeBull.Models;

namespace TradeBull.Data.Models
{
    public class Trade
    {
        public Guid Id { get; set; }

        public required Guid UserId { get; set; }

        public required Guid StockId { get; set; }

        public required DateTime RegisteredAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public required TradeType Type { get; set; }

        public required TradeCondition Condition { get; set; }

        /// <summary>
        /// Shares traded
        /// </summary>
        public required int Quantity { get; set; }

        /// <summary>
        /// This property is required if the <see cref="TradeCondition"/> is anything
        /// other than <see cref="TradeCondition.Current"/>
        /// </summary>
        public decimal? RequestedSharePrice { get; set; }

        public TradeStatus Status { get; set; } = TradeStatus.Pending;

        public decimal? SharePrice { get; set; }

        public decimal? TotalPrice { get; set; }

        public User? User { get; set; }

        public Stock? Stock { get; set; }

        // Simple mapping without AutoMapper
        public virtual TradeBull.Models.Trade ToTradeModel()
            => ToTradeModel<TradeBull.Models.Trade>(this);

        public virtual T ToTradeModel<T>() where T : TradeBull.Models.Trade, new()
            => ToTradeModel<T>(this);

        public static T ToTradeModel<T>(Trade trade) where T : TradeBull.Models.Trade, new()
        {
            ArgumentNullException.ThrowIfNull(trade);

            // Alternatively can be broken down into its own `ToSensitiveTradeModel`
            if (typeof(T) == typeof(SensitiveTrade))
            {
                return (T)(object)(new SensitiveTrade
                {
                    Id = trade.Id,
                    Type = trade.Type,
                    Status = trade.Status,
                    Condition = trade.Condition,

                    RegisteredAt = trade.RegisteredAt,
                    ProcessedAt = trade.ProcessedAt,
                    
                    Quantity = trade.Quantity,
                    RequestedSharePrice = trade.RequestedSharePrice,
                    SharePrice = trade.SharePrice,
                    TotalPrice = trade.TotalPrice
                });
            }

            return new T
            {
                Type = trade.Type,
                ProcessedAt = trade.ProcessedAt,
                Quantity = trade.Quantity,
                SharePrice = trade.SharePrice
            };
        }
    }
}