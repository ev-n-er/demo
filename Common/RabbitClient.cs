using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Common
{
    public abstract class RabbitClient : IHostedService, IDisposable
    {
        private const string EXCHANGE_NAME = "direct";
        protected abstract string QueueName { get; }
        protected abstract string[] ConsumeKeys { get; }

        private const int MaxReconnectDelayMs = 30000;
        //For plain concurrency, not suitable for high load.
        private readonly SemaphoreSlim _channelLock = new(1, 1);
        private readonly ConnectionFactory _connectionFactory;
        private volatile bool _stopping;

        private IConnection? _connection;
        private IChannel? _channel;
        

        public RabbitClient(string hostName)
        {
            _connectionFactory = new ConnectionFactory() { HostName = hostName };
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            _connection.ConnectionShutdownAsync += OnConnectionShutdown;
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await _channel.ExchangeDeclareAsync(exchange: EXCHANGE_NAME,
                               type: ExchangeType.Direct,
                               durable: false,
                               cancellationToken: cancellationToken);

            await _channel.QueueDeclareAsync(queue: QueueName,
                               durable: false,
                               exclusive: false,
                               autoDelete: false,
                               arguments: null,
                               cancellationToken: cancellationToken);

            foreach (var key in ConsumeKeys)
            {
                await _channel.QueueBindAsync(queue: QueueName,
                                  exchange: EXCHANGE_NAME,
                                  routingKey: key,
                                  cancellationToken: cancellationToken);
            }
        }

        private async Task OnConnectionShutdown(object sender, ShutdownEventArgs args)
        {
            if (args.Initiator == ShutdownInitiator.Application || _stopping)
                return;

            _channel = null;
            _connection = null;

            var delayMs = 1000;
            while (!_stopping)
            {
                try
                {
                    await ConnectAsync(CancellationToken.None);
                    return;
                }
                catch
                {
                    if (_stopping) return;
                    await Task.Delay(delayMs);
                    delayMs = Math.Min(delayMs * 2, MaxReconnectDelayMs);
                }
            }
        }

        protected async Task SendDirect(string key, object message)
        {
            if (_channel == null) return;
            var stringMessage = JsonSerializer.Serialize(message);

            var body = Encoding.UTF8.GetBytes(stringMessage);

            await _channelLock.WaitAsync();
            try
            {
                await _channel.BasicPublishAsync(EXCHANGE_NAME,
                                key,
                                true,
                                new BasicProperties { Persistent = true },
                                body);
            }
            finally
            {
                _channelLock.Release();
            }
        }

        public delegate ValueTask<bool> HandleMessage<TMessage>(string key, TMessage message);
        protected async Task Listen<TMessage>(HandleMessage<TMessage?> messageHandler, CancellationToken cancellationToken)
        {
            if (_channel == null) return;
            var consumer = new AsyncEventingBasicConsumer(_channel);

            AsyncEventHandler<BasicDeliverEventArgs> receivedHandler = async (sender, eventArgs) =>
            {
                var messageBytes = eventArgs.Body.ToArray();
                var messageString = Encoding.UTF8.GetString(messageBytes);
                var message = JsonSerializer.Deserialize<TMessage>(messageString);
                bool consumed = false;
                try
                {
                    consumed = await messageHandler(eventArgs.RoutingKey, message);
                }
                catch
                {

                }

                if (consumed)
                {
                    await _channelLock.WaitAsync();
                    try
                    {
                        await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                    }
                    finally
                    {
                        _channelLock.Release();
                    }
                }
            };

            consumer.ReceivedAsync += receivedHandler;
            await _channel.BasicConsumeAsync(QueueName, false, consumer, cancellationToken);

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException) { }

            consumer.ReceivedAsync -= receivedHandler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ConnectAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _stopping = true;

            if (_channel != null)
            {
                foreach (var key in ConsumeKeys)
                {
                    await _channel.QueueUnbindAsync(queue: QueueName,
                                      exchange: EXCHANGE_NAME,
                                      routingKey: key,
                                      cancellationToken: cancellationToken);
                }

                await _channel.QueueDeleteAsync(QueueName, cancellationToken: cancellationToken);
            }

            if (_connection != null)
            {
                _connection.ConnectionShutdownAsync -= OnConnectionShutdown;
                await _connection.CloseAsync(cancellationToken);
            }
        }

        public void Dispose()
        {
            _stopping = true;
            if (_channel != null)
            {
                _channel.Dispose();
            }
            if (_connection != null)
            {
                _connection.Dispose();
            }

        }
    }
}
