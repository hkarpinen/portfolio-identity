using System.Security.Cryptography;

namespace Domain.Aggregates.User;

/// <summary>
/// One signed-in session. The access token stays short-lived and unrevocable by design; this is
/// the record that makes a session revocable at all, and what the sessions screen lists.
///
/// Only a HASH of the token is stored. A leaked database therefore yields no usable session — the
/// raw value exists once, in the cookie of the browser it was issued to.
/// </summary>
public sealed class RefreshToken
{
    public const int LifetimeDays = 30;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    /// <summary>Set when this token was rotated, so a presented-after-rotation token is
    /// distinguishable from one that was simply signed out.</summary>
    public Guid? ReplacedById { get; private set; }

    /// <summary>Groups every token descended from one sign-in, so reuse can revoke the whole
    /// lineage rather than the single stolen link.</summary>
    public Guid FamilyId { get; private set; }

    public string? UserAgent { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime LastUsedAt { get; private set; }

    private RefreshToken() { }

    /// <summary>Returns the record to store and the raw token to put in the cookie. The raw value
    /// is returned once and never persisted.</summary>
    public static (RefreshToken Record, string RawToken) Issue(
        Guid userId,
        string? userAgent,
        string? ipAddress,
        Guid? familyId = null)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var now = DateTime.UtcNow;
        var record = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(raw),
            CreatedAt = now,
            LastUsedAt = now,
            ExpiresAt = now.AddDays(LifetimeDays),
            FamilyId = familyId ?? Guid.NewGuid(),
            UserAgent = Truncate(userAgent, 400),
            IpAddress = Truncate(ipAddress, 60)
        };
        return (record, raw);
    }

    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));

    public bool IsActive(DateTime asOf) => RevokedAt is null && asOf < ExpiresAt;

    public void Revoke(DateTime asOf) => RevokedAt ??= asOf;

    /// <summary>Rotation: this token is spent and names its successor.</summary>
    public void ReplaceWith(RefreshToken successor, DateTime asOf)
    {
        Revoke(asOf);
        ReplacedById = successor.Id;
    }

    public void Touch(DateTime asOf) => LastUsedAt = asOf;

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null
        : value.Length <= max ? value
        : value[..max];
}
