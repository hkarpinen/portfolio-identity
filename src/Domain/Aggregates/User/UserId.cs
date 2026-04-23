namespace Domain.Aggregates.User;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
}
