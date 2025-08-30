namespace TradeBull.Data.Models
{
    public class UserStock
    {
        public Guid UserId { get; set; }

        public Guid StockId { get; set; }

        public int Shares { get; set; }
    }
}
