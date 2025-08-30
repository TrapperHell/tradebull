namespace TradeBull.Messaging
{
    public interface IQueueMessaging : IDisposable, IAsyncDisposable
    {
        Task InitializeAsync(string clientName, CancellationToken cancellationToken = default);

        Task PublishMessageAsync<T>(T message);

        Task ListenForMessagesAsync<T>(Func<T, Task> receiveHandler);
    }
}
