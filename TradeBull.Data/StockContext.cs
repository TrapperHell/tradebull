using System;
using Microsoft.EntityFrameworkCore;
using TradeBull.Data.Models;
using TradeBull.Models;

namespace TradeBull.Data
{
    public class StockContext : DbContext
    {
        public StockContext(DbContextOptions<StockContext> options)
            : base(options)
        { }

        public DbSet<Market> Markets { get; set; }

        public DbSet<Models.Stock> Stocks { get; set; }

        public DbSet<StockHistory> StockHistories { get; set; }

        public DbSet<StockDayPerformance> StockDayPerformances { get; set; }

        public DbSet<Models.Trade> Trades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockDayPerformance>()
                .HasKey(x => new { x.StockId, x.Date });

            modelBuilder.Entity<StockHistory>()
                .HasKey(x => new { x.StockId, x.Date });

            modelBuilder.Entity<UserStock>()
                .HasKey(x => new { x.UserId, x.StockId });

            // One-to-many relationship between Stock <> StockDayPerformance
            modelBuilder.Entity<StockDayPerformance>()
                .HasOne(x => x.Stock)
                .WithMany(x => x.DayPerformance)
                .HasForeignKey(x => x.StockId);

            // Define an index based on StockDayPerformance's StockId and another for the Date separately
            modelBuilder.Entity<StockDayPerformance>()
                .HasIndex(x => x.StockId);

            modelBuilder.Entity<StockDayPerformance>()
                .HasIndex(x => x.Date);

            // One-to-many relationship between Stock <> StockHistory
            modelBuilder.Entity<StockHistory>()
                .HasOne(x => x.Stock)
                .WithMany(x => x.History)
                .HasForeignKey(x => x.StockId);

            // One-to-many relationship between Stock <> Market
            modelBuilder.Entity<Models.Stock>()
                .HasOne(x => x.Market)
                .WithMany(x => x.Stocks)
                .HasForeignKey(x => x.MarketId);

            // One-to-many relationship between User <> Trade
            modelBuilder.Entity<Models.Trade>()
                .HasOne(x => x.User)
                .WithMany(x => x.Trades)
                .HasForeignKey(x => x.UserId);

            // One-to-many relationship between Trade <> Stock
            modelBuilder.Entity<Models.Trade>()
                .HasOne(x => x.Stock)
                .WithMany(x => x.Trades)
                .HasForeignKey(x => x.StockId);

            // Define an index based on Trade's StockId + UserId + Status + Type
            modelBuilder.Entity<Models.Trade>()
                .HasIndex(x => new { x.StockId, x.UserId, x.Status, x.Type });

            // Define an index based on Trade's RegisteredAt
            modelBuilder.Entity<Models.Trade>()
                .HasIndex(x => x.RegisteredAt);

            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // HACK: The below data has no place being available in a Production environment, but it's useful for the purpose of this task
            Guid userId = Guid.NewGuid(), marketId = Guid.NewGuid(), stockId = Guid.NewGuid();

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = userId,
                    Username = "Test User",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    EmailAddress = "test@test.com",
                    AccountBalance = 1_000_000,
                    PasswordHash = "xxx"
                },
                new User
                {
                    Id = Constants.DefaultUserId,
                    Username = "Active User",
                    CreatedAt = DateTime.UtcNow,
                    EmailAddress = "admin@test.com",
                    AccountBalance = 10_000_000,
                    PasswordHash = "xxx"
                }
            );

            modelBuilder.Entity<Market>().HasData(
                new Market
                {
                    Id = marketId,
                    Name = Constants.DefaultMarketName,
                    OpensAt = new TimeOnly(1, 0, 0),
                    ClosesAt = new TimeOnly(23, 0, 0)
                }
            );

            modelBuilder.Entity<Models.Stock>().HasData(
                new Models.Stock
                {
                    Id = stockId,
                    MarketId = marketId,
                    EntityName = "Microsoft Corporation",
                    TickerSymbol = "MSFT"
                }
            );

            modelBuilder.Entity<StockDayPerformance>().HasData(
                new StockDayPerformance
                {
                    StockId = stockId,
                    Date = DateTime.UtcNow,
                    Price = 500
                }
            );

            modelBuilder.Entity<Models.Trade>().HasData(
                new Models.Trade
                {
                    Id = Guid.NewGuid(),
                    StockId = stockId,
                    UserId = userId,
                    RegisteredAt = DateTime.UtcNow.AddHours(-12),
                    ProcessedAt = DateTime.UtcNow.AddHours(-6),
                    Type = TradeType.Buy,
                    Condition = TradeCondition.Current,
                    Status = TradeStatus.Completed,
                    SharePrice = 499.5m,
                    Quantity = 100,
                    TotalPrice = (499.5m * 100) + Constants.TradeFlatFee
                },
                new Models.Trade
                {
                    Id = Guid.NewGuid(),
                    StockId = stockId,
                    UserId = userId,
                    RegisteredAt = DateTime.UtcNow.AddHours(-2),
                    Type = TradeType.Buy,
                    Condition = TradeCondition.Current,
                    Status = TradeStatus.Pending,
                    Quantity = 50
                },
                new Models.Trade
                {
                    Id = Guid.NewGuid(),
                    StockId = stockId,
                    UserId = userId,
                    RegisteredAt = DateTime.UtcNow.AddHours(-1),
                    Type = TradeType.Sell,
                    Condition = TradeCondition.Current,
                    Status = TradeStatus.Pending,
                    Quantity = 50
                }
            );

            modelBuilder.Entity<UserStock>().HasData(
                new UserStock
                {
                    UserId = userId,
                    StockId = stockId,
                    Shares = 100
                });
        }
    }
}
