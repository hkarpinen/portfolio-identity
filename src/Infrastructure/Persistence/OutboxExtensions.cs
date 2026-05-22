using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal static class OutboxExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void AddToOutbox(this DbContext context, IDomainEvent domainEvent)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = domainEvent.GetType().Name,
            Payload = JsonSerializer.Serialize<object>(domainEvent, JsonOptions),
            CreatedAt = DateTime.UtcNow,
            Published = false
        };

        context.Set<OutboxMessage>().Add(message);
    }
}
