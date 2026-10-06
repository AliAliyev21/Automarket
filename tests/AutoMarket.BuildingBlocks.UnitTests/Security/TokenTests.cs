using System.Buffers.Text;
using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.BuildingBlocks.UnitTests.Security;

// SEC-AUTH-05/07: ≥ 256 bit təsadüfi token, serverdə SHA-256, sabit vaxtda müqayisə
public sealed class TokenTests
{
    [Fact]
    public void Generate_Token_Is256BitBase64Url()
    {
        var token = new SecureTokenGenerator().Generate();

        Base64Url.DecodeFromChars(token).Length.ShouldBe(32);
        token.ShouldNotContain('+');
        token.ShouldNotContain('/');
        token.ShouldNotContain('=');
    }

    [Fact]
    public void Generate_TwoTokens_Differ()
    {
        var generator = new SecureTokenGenerator();

        generator.Generate().ShouldNotBe(generator.Generate());
    }

    [Fact]
    public void Hash_Token_Is32Bytes() => TokenHasher.Hash("token").Length.ShouldBe(32);

    [Fact]
    public void Matches_SameToken_True() => TokenHasher.Matches("token", TokenHasher.Hash("token")).ShouldBeTrue();

    [Fact]
    public void Matches_DifferentToken_False() => TokenHasher.Matches("token2", TokenHasher.Hash("token")).ShouldBeFalse();

    [Fact]
    public void ForEmail_RateLimitKey_IsHashNotRawEmail()
    {
        var key = RateLimitKeys.ForEmail("leyla@automarket.az");

        key.ShouldNotContain("leyla");
        key.Length.ShouldBe(64);
    }
}
