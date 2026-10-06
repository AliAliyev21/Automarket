using System.Security.Cryptography;
using AutoMarket.Identity.Infrastructure.Tokens;

namespace AutoMarket.Identity.UnitTests.Infrastructure.Tokens;

// SEC-SEC-03: zəif, placeholder və ya boş açarla tətbiq işə düşmür; xəta mesajında açar yoxdur
public sealed class JwtOptionsValidatorTests
{
    [Fact]
    public void Validate_StrongKey_Succeeds() =>
        Validate(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))).Succeeded.ShouldBeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("change-me-jwt-key")]
    [InlineData("<base64 key>")]
    [InlineData("not base64 !!")]
    public void Validate_InvalidKey_Fails(string key)
    {
        var result = Validate(key);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Jwt:SigningKeys:0:Key");
    }

    [Fact]
    public void Validate_KeyShorterThan256Bits_FailsWithoutLeakingKey()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(31));

        var result = Validate(key);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotContain(key);
    }

    [Fact]
    public void Validate_LifetimeLongerThanHour_Fails() =>
        Validate(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)), TimeSpan.FromHours(2)).Failed.ShouldBeTrue();

    private static Microsoft.Extensions.Options.ValidateOptionsResult Validate(string key, TimeSpan? lifetime = null)
    {
        var options = new JwtOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            AccessTokenLifetime = lifetime ?? TimeSpan.FromMinutes(15),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        options.SigningKeys.Add(new JwtSigningKeyOptions { KeyId = "k1", Key = key });

        return new JwtOptionsValidator().Validate(null, options);
    }
}
