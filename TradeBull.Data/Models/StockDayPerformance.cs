namespace TradeBull.Data.Models
{
    public class StockDayPerformance
    {
        public required Guid StockId { get; set; }

        public required DateTime Date { get; set; }

        public decimal Price { get; set; }

        public Stock? Stock { get; set; }
    }
}