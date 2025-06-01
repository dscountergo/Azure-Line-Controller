using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Shared
{
    public class RabbitMQService : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string ExchangeName = "device_logs";
        private const string QueueName = "device_logs_queue";
        private const string RoutingKey = "device.logs";

        public RabbitMQService(string hostName = "localhost")
        {
            var factory = new ConnectionFactory { HostName = hostName };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Deklaracja exchange typu topic
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);

            // Deklaracja kolejki
            _channel.QueueDeclare(QueueName, true, false, false, null);

            // Powiązanie kolejki z exchange
            _channel.QueueBind(QueueName, ExchangeName, RoutingKey);
        }

        public void PublishLog(string deviceId, string message)
        {
            var logMessage = new
            {
                DeviceId = deviceId,
                Timestamp = DateTime.UtcNow,
                Message = message
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(logMessage));
            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: RoutingKey,
                basicProperties: null,
                body: body);
        }

        public void SubscribeToLogs(Action<string, string> onLogReceived)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var logMessage = JsonSerializer.Deserialize<LogMessage>(message);

                if (logMessage != null)
                {
                    onLogReceived(logMessage.DeviceId, logMessage.Message);
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: true,
                consumer: consumer);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }

        private class LogMessage
        {
            public string DeviceId { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }
} 