using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Aggregates.User;
using Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>Serialises UserId value object as a plain Guid so that the JSON
/// stored in outbox_messages matches the flat wire-message shape.</summary>
internal sealed class UserIdJsonConverter : JsonConverter<UserId>
{
    public override UserId Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
        => new(reader.GetGuid());

    public override void Write(Utf8JsonWriter writer, UserId value, JsonSerializerOptions o)
        => writer.WriteStringValue(value.Value);
}

internal static class OutboxExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new UserIdJsonConverter() }
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
