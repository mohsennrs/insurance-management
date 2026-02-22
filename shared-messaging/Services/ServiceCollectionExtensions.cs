using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using shared_messaging.Events;
namespace shared_messaging.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add RabbitMQ event bus to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="exchangeName">Exchange name (default: claimflow-events)</param>
    public static IServiceCollection AddRabbitMQEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        string exchangeName = "claimflow-events")
    {
        var connectionString = configuration["RabbitMQ:ConnectionString"]
            ?? throw new InvalidOperationException("RabbitMQ:ConnectionString is required");

        services.AddSingleton<IEventBus>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RabbitMQEventBus>>();
            return new RabbitMQEventBus(connectionString, exchangeName, sp, logger);
        });

        return services;
    }
}