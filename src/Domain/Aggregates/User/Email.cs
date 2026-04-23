using System.Text.RegularExpressions;

namespace Domain.Aggregates.User;

public sealed partial record Email
{
    private static readonly Regex EmailRegex = MyEmailRegex();

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalized))
            throw new ArgumentException($"Invalid email format: '{value}'.", nameof(value));

        return new Email(normalized);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex MyEmailRegex();
}
