using shared_messaging.Events;

namespace shared_messaging.Events;

/// <summary>
/// Interface for publishing and subscribing to events
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publish an event to the message broker
    /// </summary>
    Task PublishAsync<T>(T @event) where T : IntegrationEvent;

    /// <summary>
    /// Subscribe to an event type
    /// </summary>
    void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IEventHandler<T>;
}

/// <summary>
/// Interface for handling events
/// </summary>
public interface IEventHandler<in T> where T : IntegrationEvent
{
    Task HandleAsync(T @event);
}