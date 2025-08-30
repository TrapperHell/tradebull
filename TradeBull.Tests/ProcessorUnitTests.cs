using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TradeBull.Messaging;
using TradeBull.Processor;

namespace TradeBull.Tests
{
    public class ProcessorUnitTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IQueueMessaging _queueMessaging;

        public ProcessorUnitTests(ServiceProviderFixture fixture)
        {
            _serviceProvider = fixture.ServiceProvider;

            var mock = new Mock<IQueueMessaging>();
            mock.Setup(x => x.InitializeAsync(string.Empty, CancellationToken.None)).Returns(Task.CompletedTask);
            mock.Setup(x => x.ListenForMessagesAsync<Data.Models.Trade>(default!)).Returns(Task.CompletedTask);
            _queueMessaging = mock.Object;
        }

        [Fact]
        public void BuyTrade_WithSufficientBalance_ShouldSucced()
        {
            // Arrange
            var worker = new WorkerService(_serviceProvider, _queueMessaging, _serviceProvider.GetRequiredService<ILogger<WorkerService>>());

            var trade = new Data.Models.Trade
            {
                RegisteredAt = DateTime.UtcNow,
                Condition = Models.TradeCondition.Current,
                StockId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Type = Models.TradeType.Buy,
                Quantity = 1,
                SharePrice = 1,
                User = new Data.Models.User
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    EmailAddress = string.Empty,
                    PasswordHash = string.Empty,
                    Username = string.Empty,
                    AccountBalance = 2
                }
            };

            // Act
            var result = worker.ProcessTrade(ref trade, 1);

            // Assert
            Assert.True(result);
            Assert.NotNull(trade.ProcessedAt);
            Assert.Equal(Models.TradeStatus.Completed, trade.Status);
            Assert.Equal(0, trade.User!.AccountBalance);
        }

        [Fact]
        public void BuyTrade_WithInsufficientBalance_ShouldFail()
        {
            // Arrange
            var worker = new WorkerService(_serviceProvider, _queueMessaging, _serviceProvider.GetRequiredService<ILogger<WorkerService>>());
            var initialAccountBalance = 1;
            var trade = new Data.Models.Trade
            {
                RegisteredAt = DateTime.UtcNow,
                Condition = Models.TradeCondition.Current,
                StockId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Type = Models.TradeType.Buy,
                Quantity = 1,
                SharePrice = 1,
                User = new Data.Models.User
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    EmailAddress = string.Empty,
                    PasswordHash = string.Empty,
                    Username = string.Empty,
                    AccountBalance = initialAccountBalance
                }
            };

            // Act
            var result = worker.ProcessTrade(ref trade, 1);

            // Assert
            Assert.False(result);
            Assert.Null(trade.ProcessedAt);
            Assert.Equal(Models.TradeStatus.Pending, trade.Status);
            Assert.Equal(initialAccountBalance, trade.User!.AccountBalance);
        }
    }
}
