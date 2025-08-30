namespace TradeBull.Data.Models
{
    public class StockHistory
    {
        public required Guid StockId { get; set; }

        public required DateOnly Date { get; set; }

        public decimal HighestPrice { get; set; }

        public decimal LowestPrice { get; set; }

        public decimal OpenPrice { get; set; }

        public decimal ClosePrice { get; set; }

        public int TradeVolume { get; set; }

        public Stock? Stock { get; set; }
    }
}