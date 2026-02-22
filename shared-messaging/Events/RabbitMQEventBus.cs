using System.Text;
using System.Text.Json;
using shared_messaging.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace shared_messaging.Events;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly string _exchangeName;
    private readonly Dictionary<string, Type> _eventHandlers;

    public RabbitMQEventBus(
        string connectionString,
        string exchangeName,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _exchangeName = exchangeName;
        _eventHandlers = new Dictionary<string, Type>();

        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        try
        {
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declare the exchange (topic for routing by event name)
            _channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false).GetAwaiter().GetResult();

            _logger.LogInformation("Connected to RabbitMQ at {Host}", factory.Uri.Host);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    public async Task PublishAsync<T>(T @event) where T : IntegrationEvent
    {
        var eventName = typeof(T).Name;
        var routingKey = eventName;

        try
        {
            var message = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = eventName,
                MessageId = @event.EventId.ToString(),
                Timestamp = new AmqpTimestamp(
                    new DateTimeOffset(@event.CreatedAt).ToUnixTimeSeconds())
            };

            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Published event {EventName} with ID {EventId}",
                eventName,
                @event.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventName}", eventName);
            throw;
        }
    }

    public void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IEventHandler<T>
    {
        var eventName = typeof(T).Name;
        var handlerType = typeof(TH);

        if (_eventHandlers.ContainsKey(eventName))
        {
            _logger.LogWarning(
                "Handler {HandlerType} already registered for event {EventName}",
                handlerType.Name,
                eventName);
            return;
        }

        _eventHandlers.Add(eventName, handlerType);

        // Create a queue for this subscriber (auto-delete when service stops)
        var queueName = $"{eventName}.{Environment.MachineName}.{Guid.NewGuid()}";

        _channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: true,
            autoDelete: true).GetAwaiter().GetResult();

        _channel.QueueBindAsync(
            queue: queueName,
            exchange: _exchangeName,
            routingKey: eventName).GetAwaiter().GetResult();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var eventName = eventArgs.BasicProperties.Type ?? string.Empty;
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            try
            {
                await ProcessEventAsync(eventName, message);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventName}", eventName);
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer).GetAwaiter().GetResult();

        _logger.LogInformation(
            "Subscribed to event {EventName} with handler {HandlerType}",
            eventName,
            handlerType.Name);
    }

    private async Task ProcessEventAsync(string eventName, string message)
    {
        if (!_eventHandlers.ContainsKey(eventName))
        {
            _logger.LogWarning("No handler found for event {EventName}", eventName);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var handlerType = _eventHandlers[eventName];
        var handler = scope.ServiceProvider.GetService(handlerType);

        if (handler == null)
        {
            _logger.LogError("Could not resolve handler {HandlerType}", handlerType.Name);
            return;
        }

        // Get the event type from the handler's implemented interface IEventHandler<T>
        var eventType = handlerType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
            ?.GetGenericArguments()
            .FirstOrDefault();

        if (eventType == null)
        {
            _logger.LogError("Could not determine event type for {EventName}", eventName);
            return;
        }

        var @event = JsonSerializer.Deserialize(message, eventType, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (@event == null)
        {
            _logger.LogError("Failed to deserialize event {EventName}", eventName);
            return;
        }

        var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var method = concreteType.GetMethod("HandleAsync");

        if (method != null)
        {
            await (Task)method.Invoke(handler, new[] { @event })!;
        }
    }

    public void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _channel?.Dispose();
        _connection?.CloseAsync().GetAwaiter().GetResult();
        _connection?.Dispose();
    }
}
