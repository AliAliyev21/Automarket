using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Domain.Tokens;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = TestData.Now;
    private static readonly TimeSpan Sliding = TimeSpan.FromDays(14);

    [Fact]
    public void Issue_FarFromAbsolute_ExpiresAfterSlidingLifetime()
    {
        var token = Issue(absoluteExpiresAt: Now.AddDays(60));

        token.ExpiresAt.ShouldBe(Now.AddDays(14));
        token.AbsoluteExpiresAt.ShouldBe(Now.AddDays(60));
        token.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void Issue_NearAbsolute_ExpiresAtAbsolute()
    {
        // Q3: sliding müddət mütləq maksimumu (60 gün) keçə bilməz
        var token = Issue(absoluteExpiresAt: Now.AddDays(3));

        token.ExpiresAt.ShouldBe(Now.AddDays(3));
    }

    [Fact]
    public void IsActive_AfterSlidingExpiry_False()
    {
        var token = Issue(absoluteExpiresAt: Now.AddDays(60));

        token.IsActive(Now.AddDays(14)).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_Active_NotActiveAndReasonKept()
    {
        var token = Issue(absoluteExpiresAt: Now.AddDays(60));

        token.Revoke(Now, RefreshTokenRevocationReason.SessionLimit);
        token.Revoke(Now.AddMinutes(1), RefreshTokenRevocationReason.ReuseDetected);

        token.IsActive(Now).ShouldBeFalse();
        token.RevokedAt.ShouldBe(Now);
        token.RevokedReason.ShouldBe(RefreshTokenRevocationReason.SessionLimit);
        token.WasRotated.ShouldBeFalse();
    }

    private static RefreshToken Issue(DateTimeOffset absoluteExpiresAt) => RefreshToken.Issue(
        Guid.CreateVersion7(Now),
        Guid.CreateVersion7(Now),
        Guid.CreateVersion7(Now),
        parentId: null,
        new byte[32],
        Now,
        Sliding,
        absoluteExpiresAt,
        "127.0.0.1",
        "tests");
}
