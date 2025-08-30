namespace TradeBull.Data.Models
{
    public class User
    {
        public required Guid Id { get; set; }

        public required string EmailAddress { get; set; }

        public required string Username { get; set; }

        public required string PasswordHash { get; set; }

        public required decimal AccountBalance { get; set; }

        public required DateTime CreatedAt { get; set; }

        public ICollection<Trade>? Trades { get; set; }
    }
}