namespace TradeBull.Data.Models
{
    public class Market
    {
        public required Guid Id { get; set; }

        public required string Name { get; set; }

        /* NOTE: Stock Markets typically only open on weekdays, excluding (some) public holidays
         * but for the sake of this exercise let's pretend that they open daily.
        */
        public required TimeOnly OpensAt { get; set; }

        public required TimeOnly ClosesAt { get; set; }

        public ICollection<Stock>? Stocks { get; set; }
    }
}