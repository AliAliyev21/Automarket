using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Domain.Tokens;

// SEC-AUTH-07, FR-AUTH-02 AC1: birdəfəlik, vaxtı məhdud
public sealed class OneTimeTokenTests
{
    private static readonly DateTimeOffset Now = TestData.Now;

    [Fact]
    public void IsUsable_BeforeExpiry_True() => Issue().IsUsable(Now.AddHours(23)).ShouldBeTrue();

    [Fact]
    public void IsUsable_AtExpiry_False() => Issue().IsUsable(Now.AddHours(24)).ShouldBeFalse();

    [Fact]
    public void Use_Twice_SecondThrows()
    {
        var token = Issue();
        token.Use(Now);

        token.IsUsable(Now).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => token.Use(Now));
    }

    [Fact]
    public void Revoke_Usable_NotUsable()
    {
        var token = Issue();

        token.Revoke(Now);

        token.IsUsable(Now).ShouldBeFalse();
    }

    private static OneTimeToken Issue() => OneTimeToken.Issue(
        Guid.CreateVersion7(Now),
        Guid.CreateVersion7(Now),
        OneTimeTokenPurpose.EmailConfirmation,
        new byte[32],
        Now,
        Now.AddHours(24));
}
