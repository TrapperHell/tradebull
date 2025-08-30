using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TradeBull.Models.Requests;

namespace TradeBull.Tests
{
    public class ApiTests : IClassFixture<WebApplicationFactory<Api.Program>>
    {
        private readonly HttpClient _client;

        public ApiTests(WebApplicationFactory<Api.Program> webFactory)
        {
            _client = webFactory.CreateClient();
        }

        [Fact]
        public async Task GetTrades_ShouldSucceed()
        {
            // Arrange
            var url = "api/v1/Stock/MSFT/trades";

            // Act
            using var response = await _client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
        }

        [Fact]
        public async Task Trade_ShouldSucceed()
        {
            // Arrange
            var url = "api/v1/Stock/MSFT/trade";

            // Act
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            using var requestContent = new StringContent(JsonSerializer.Serialize(new TradeRequest
            {
                Type = Models.TradeType.Buy,
                Condition = Models.TradeCondition.Current,
                Quantity = 10
            }), System.Text.Encoding.UTF8, "application/json");
            request.Content = requestContent;
            using var response = await _client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
        }

        [Fact]
        public async Task Trade_WithLimitCondition_ShouldFail()
        {
            // Arrange
            var url = "api/v1/Stock/MSFT/trade";

            // Act
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            using var requestContent = new StringContent(JsonSerializer.Serialize(new TradeRequest
            {
                Type = Models.TradeType.Buy,
                Condition = Models.TradeCondition.Fall,
                Quantity = 10
            }), System.Text.Encoding.UTF8, "application/json");
            request.Content = requestContent;
            using var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}