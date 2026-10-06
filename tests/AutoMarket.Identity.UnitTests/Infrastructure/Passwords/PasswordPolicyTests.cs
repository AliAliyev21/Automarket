using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Infrastructure.Passwords;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Infrastructure.Passwords;

// SEC-AUTH-01: real embedded top-100k siyahısı ilə
public sealed class PasswordPolicyTests
{
    private const string Email = "leyla.aliyeva@automarket.az";
    private const string Name = "Leyla Əliyeva";

    private static readonly PasswordPolicy Policy = new(TestData.Options());

    [Theory]
    [InlineData("çəmənlikdə gəzinti", "unicode + space")]
    [InlineData("correct horse battery", "passphrase")]
    [InlineData("Qırmızı-Maşın-2026", "azerbaijani letters")]
    public void Validate_StrongPassword_NoViolations(string password, string reason)
    {
        Policy.Validate(password, Email, Name).ShouldBeEmpty(reason);
    }

    [Fact]
    public void Validate_NineCharacters_TooShort() =>
        Codes("abcdefgh9").ShouldBe([ValidationCodes.PasswordTooShort]);

    [Fact]
    public void Validate_TenCharacters_Accepted() =>
        Codes("xk3!pq9#za").ShouldBeEmpty();

    [Fact]
    public void Validate_128Characters_Accepted() =>
        Codes(new string('ğ', 120) + "x7!kq2#z").ShouldBeEmpty();

    [Fact]
    public void Validate_129Characters_TooLong() =>
        Codes(new string('ğ', 121) + "x7!kq2#z").ShouldBe([ValidationCodes.PasswordTooLong]);

    [Fact]
    public void Validate_LengthCountsCodePoints_SurrogatePairsCountOnce()
    {
        // 10 emoji = 20 UTF-16 simvolu, amma 10 code point
        var tenEmoji = string.Concat(Enumerable.Repeat("🚗", 10));

        Codes(tenEmoji).ShouldBeEmpty();
        Codes(string.Concat(Enumerable.Repeat("🚗", 9))).ShouldBe([ValidationCodes.PasswordTooShort]);
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("qwertyuiop")]
    [InlineData("QWERTYUIOP")]
    public void Validate_CommonPassword_TooCommon(string password) =>
        Codes(password).ShouldContain(ValidationCodes.PasswordTooCommon);

    [Theory]
    [InlineData("leyla.aliyeva@automarket.az")]
    [InlineData("xx-LEYLA.ALIYEVA-2026")]
    [InlineData("my leyla əliyeva pass")]
    [InlineData("LEYLA ƏLIYEVA!!")]
    public void Validate_ContainsEmailOrName_PersonalInfo(string password) =>
        Codes(password).ShouldContain(ValidationCodes.PasswordContainsPersonalInfo);

    [Fact]
    public void Validate_ShortNameInsidePassword_Allowed()
    {
        // 2 simvollu ad yalnız bərabərlik ilə yoxlanılır
        Policy.Validate("al-x7!kq2#z9", "x@test.az", "Al").ShouldBeEmpty();
    }

    private static string[] Codes(string password) =>
        [.. Policy.Validate(password, Email, Name).Select(violation => violation.Code)];
}
