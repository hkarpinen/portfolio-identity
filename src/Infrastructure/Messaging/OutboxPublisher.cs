using System.Text.Json;
using Domain.Events;
using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

internal sealed class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisher> _logger;

    // Keys match IDomainEvent type names stored by OutboxExtensions.AddToOutbox.
    // Values are (wire-message type, queue name).
    // We Send to a named durable queue instead of Publish to an exchange so
    // messages are buffered by RabbitMQ even when consumers aren't yet running.
    // Queue names must match the kebab-case endpoint names registered by each
    // consumer service (MassTransit SetKebabCaseEndpointNameFormatter).
    private static readonly Dictionary<string, (Type MessageType, string Queue)> EventTypeMap = new()
    {
        [nameof(UserRegistered)]      = (typeof(UserRegisteredEvent),      "user-registered"),
        [nameof(UserProfileUpdated)]  = (typeof(UserProfileUpdatedEvent),  "user-profile-updated"),
        [nameof(UserBanned)]          = (typeof(UserBannedEvent),          "user-banned"),
        [nameof(DemoUserCreated)]     = (typeof(DemoUserCreatedEvent),     "demo-user-created"),
        [nameof(DemoUserExpired)]     = (typeof(DemoUserExpiredEvent),     "demo-user-expired"),
    };

    // Must match OutboxExtensions.JsonOptions so Deserialize succeeds.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OutboxPublisher(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var sendEndpointProvider = scope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();
        var rabbitHost = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()
            ["RabbitMq:Host"] ?? "localhost";

        // Begin an explicit transaction so that FOR UPDATE SKIP LOCKED holds row
        // locks until SaveChangesAsync + CommitAsync. Without a transaction the
        // lock is released immediately, which would not prevent a second replica
        // from selecting the same rows in a concurrent poll cycle.
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // FOR UPDATE SKIP LOCKED ensures that when identity is scaled to 2+
        // replicas each instance claims a disjoint set of rows, preventing
        // duplicate sends without requiring a distributed lock.
        var messages = await dbContext.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM identity.outbox_messages
                WHERE published = false AND dead_lettered = false
                ORDER BY created_at
                LIMIT 50
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            if (!EventTypeMap.TryGetValue(message.EventType, out var entry))
            {
                _logger.LogWarning("Unknown event type {EventType} on message {Id} — dead-lettering", message.EventType, message.Id);
                message.DeadLettered = true;
                message.LastError = $"Unknown event type: {message.EventType}";
                message.LastAttemptAt = DateTime.UtcNow;
                continue;
            }

            try
            {
                var @event = JsonSerializer.Deserialize(message.Payload, entry.MessageType, JsonOptions);
                if (@event is null)
                {
                    message.DeadLettered = true;
                    message.LastError = "Payload deserialized to null";
                    message.LastAttemptAt = DateTime.UtcNow;
                    continue;
                }

                // Send to the named durable queue so messages are buffered by
                // RabbitMQ even when the consumer service isn't running yet.
                var endpoint = await sendEndpointProvider.GetSendEndpoint(
                    new Uri($"rabbitmq://{rabbitHost}/{entry.Queue}"));
                await endpoint.Send(@event, entry.MessageType, cancellationToken);

                message.Published = true;
                message.PublishedAt = DateTime.UtcNow;
                message.LastAttemptAt = DateTime.UtcNow;

                _logger.LogInformation("Sent outbox message {Id} of type {EventType} to queue {Queue}",
                    message.Id, message.EventType, entry.Queue);
            }
            catch (Exception ex)
            {
                // Poison-message handling: increment retry count, dead-letter once max exceeded.
                // Crucially we do NOT break — one bad message must not block the queue.
                message.RetryCount++;
                message.LastError = ex.Message.Length > 2048 ? ex.Message[..2048] : ex.Message;
                message.LastAttemptAt = DateTime.UtcNow;

                if (message.RetryCount >= OutboxMessage.MaxRetryCount)
                {
                    message.DeadLettered = true;
                    _logger.LogError(ex,
                        "Outbox message {Id} exceeded {Max} retries — dead-lettered",
                        message.Id, OutboxMessage.MaxRetryCount);
                }
                else
                {
                    _logger.LogWarning(ex,
                        "Failed to send outbox message {Id} (attempt {Attempt}/{Max})",
                        message.Id, message.RetryCount, OutboxMessage.MaxRetryCount);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
