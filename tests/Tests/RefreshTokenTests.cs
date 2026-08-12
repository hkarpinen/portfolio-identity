using Domain.Aggregates.User;

namespace Tests;

public class RefreshTokenTests
{
    private static (RefreshToken Record, string Raw) Issue() =>
        RefreshToken.Issue(Guid.NewGuid(), "Firefox", "127.0.0.1");

    [Fact]
    public void Issue_StoresOnlyAHashOfTheToken()
    {
        var (record, raw) = Issue();

        Assert.DoesNotContain(raw, record.TokenHash);
        Assert.Equal(RefreshToken.Hash(raw), record.TokenHash);
        Assert.True(record.IsActive(DateTime.UtcNow));
    }

    [Fact]
    public void Issue_MintsADifferentTokenEveryTime()
    {
        var (_, first) = Issue();
        var (_, second) = Issue();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Issue_StartsItsOwnFamily()
    {
        var (a, _) = Issue();
        var (b, _) = Issue();

        Assert.NotEqual(a.FamilyId, b.FamilyId);
    }

    [Fact]
    public void Issue_LastsThirtyDays()
    {
        var (record, _) = Issue();

        Assert.InRange((record.ExpiresAt - DateTime.UtcNow).TotalDays, 29.9, 30.0);
        Assert.False(record.IsActive(DateTime.UtcNow.AddDays(31)));
    }

    [Fact]
    public void ReplaceWith_SpendsTheOldTokenAndNamesItsSuccessor()
    {
        var (first, _) = Issue();
        var (second, _) = RefreshToken.Issue(first.UserId, null, null, first.FamilyId);

        first.ReplaceWith(second, DateTime.UtcNow);

        Assert.False(first.IsActive(DateTime.UtcNow));
        Assert.Equal(second.Id, first.ReplacedById);
        // Same lineage, so reuse of the spent token can revoke everything descended from it.
        Assert.Equal(first.FamilyId, second.FamilyId);
    }

    [Fact]
    public void Revoke_KeepsTheFirstTime()
    {
        var (record, _) = Issue();
        var first = DateTime.UtcNow;

        record.Revoke(first);
        record.Revoke(first.AddHours(1));

        Assert.Equal(first, record.RevokedAt);
    }

    [Fact]
    public void RevokedWithoutReplacement_IsASignOut_NotReuse()
    {
        var (record, _) = Issue();

        record.Revoke(DateTime.UtcNow);

        Assert.False(record.IsActive(DateTime.UtcNow));
        Assert.Null(record.ReplacedById);
    }

    [Fact]
    public void Revoke_IsIdempotent_SoARepeatedSweepCannotMoveTheTime()
    {
        // The rule that a bulk UPDATE in the repository was quietly bypassing: revoking twice
        // keeps the FIRST time, because that is when the session actually ended.
        var (record, _) = Issue();
        var ended = DateTime.UtcNow;

        record.Revoke(ended);
        record.Revoke(ended.AddMinutes(30));

        Assert.Equal(ended, record.RevokedAt);
    }
}
