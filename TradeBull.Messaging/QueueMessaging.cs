using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TradeBull.Messaging
{
    public class QueueMessaging : IQueueMessaging
    {
        private readonly Options.Connection _options;
        private IConnection? _connection = null;
        private IChannel? _channel = null;
        private AsyncEventingBasicConsumer? _consumer = null;
        private string? _consumerTag = null;
        private bool _disposed = false;
        private readonly JsonSerializerOptions _serializerOptions;

        public QueueMessaging(IOptions<Options.Connection> options)
        {
            _options = options.Value;

            _serializerOptions = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve };
        }

        public async Task InitializeAsync(string clientName, CancellationToken cancellationToken = default)
        {
            if (_connection != null || _channel != null)
                throw new InvalidOperationException($"{nameof(QueueMessaging)} has already been initialized.");

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_options.Uri),
                ClientProvidedName = clientName
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Direct, cancellationToken: cancellationToken);
            await _channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
            await _channel.QueueBindAsync(_options.QueueName, _options.ExchangeName, _options.RoutingKey, cancellationToken: cancellationToken);
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, false);
        }

        public async Task PublishMessageAsync<T>(T message)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (_channel == null)
                throw new InvalidOperationException($"{nameof(QueueMessaging)} must be initialized before use.");

            var json = JsonSerializer.Serialize(message, _serializerOptions);
            await _channel.BasicPublishAsync(_options.ExchangeName, _options.RoutingKey, Encoding.UTF8.GetBytes(json));
        }

        public async Task ListenForMessagesAsync<T>(Func<T, Task> receiveHandler)
        {
            ArgumentNullException.ThrowIfNull(receiveHandler);

            if (_channel == null)
                throw new InvalidOperationException($"{nameof(QueueMessaging)} must be initialized before use.");

            if (_consumer != null)
                throw new InvalidOperationException($"{nameof(QueueMessaging)} is already registered to listen for messages.");

            _consumer = new AsyncEventingBasicConsumer(_channel);

            _consumer.ReceivedAsync += async (sender, args) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(args.Body.ToArray());
                    var result = JsonSerializer.Deserialize<T>(json, _serializerOptions);
                    await receiveHandler(result);

                    await _channel.BasicAckAsync(args.DeliveryTag, false);
                }
                catch (Exception)
                {
                    await _channel.BasicNackAsync(args.DeliveryTag, false, true);
                }

                await Task.CompletedTask;
            };

            _consumerTag = await _channel.BasicConsumeAsync(_options.QueueName, autoAck: false, _consumer);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _channel?.Dispose();
                _connection?.Dispose();
            }

            _disposed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            if (_channel != null && _consumerTag != null)
                await _channel.BasicCancelAsync(_consumerTag);
            if (_channel != null)
                await _channel.DisposeAsync();
            if (_connection != null)
                await _connection.DisposeAsync();

            Dispose(false);
            GC.SuppressFinalize(this);
        }
    }
}
