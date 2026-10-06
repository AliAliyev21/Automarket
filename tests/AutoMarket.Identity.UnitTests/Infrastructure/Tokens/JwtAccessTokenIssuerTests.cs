using System.Security.Cryptography;
using AutoMarket.BuildingBlocks.Application;
using AutoMarket.Identity.Infrastructure.Tokens;
using AutoMarket.Identity.UnitTests.Builders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AutoMarket.Identity.UnitTests.Infrastructure.Tokens;

// SEC-AUTH-04, SEC-SEC-05
public sealed class JwtAccessTokenIssuerTests
{
    private static readonly Guid UserId = Guid.CreateVersion7(TestData.Now);

    [Fact]
    public void Issue_User_ContainsOnlyMinimumClaims()
    {
        var (issuer, _, _) = Create();

        var token = new JsonWebToken(issuer.Issue(UserId, [Roles.User]).Token);

        token.Claims.Select(claim => claim.Type).Distinct().Order().ShouldBe(["aud", "exp", "iat", "iss", "jti", "role", "sub"]);
        token.Subject.ShouldBe(UserId.ToString());
        token.GetClaim("role").Value.ShouldBe(Roles.User);
        token.Alg.ShouldBe(SecurityAlgorithms.HmacSha256);
        token.Kid.ShouldBe("current");
    }

    [Fact]
    public void Issue_Always_ExpiresAfterConfiguredLifetime()
    {
        var (issuer, _, time) = Create();

        var accessToken = issuer.Issue(UserId, [Roles.User]);

        accessToken.ExpiresAt.ShouldBe(time.GetUtcNow().AddMinutes(15));
        new JsonWebToken(accessToken.Token).ValidTo.ShouldBe(time.GetUtcNow().AddMinutes(15).UtcDateTime);
    }

    [Fact]
    public async Task Issue_ValidatedWithKeyRing_IsValid()
    {
        var (issuer, keyRing, _) = Create();
        var token = issuer.Issue(UserId, [Roles.User, Roles.Moderator]).Token;

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "issuer",
            ValidAudience = "audience",
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            IssuerSigningKeyResolver = (_, _, kid, _) => keyRing.GetValidationKeys(kid),
            ValidateLifetime = false,
        });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void GetSigningCredentials_NewerKeyActive_SignsWithNewestAndValidatesBoth()
    {
        // SEC-SEC-05: keçid dövründə köhnə və yeni açar paralel qəbul olunur
        var (_, keyRing, time) = Create(extraKey: new JwtSigningKeyOptions
        {
            KeyId = "next",
            Key = NewKey(),
            NotBefore = TestData.Now.AddDays(-1),
        });

        keyRing.GetSigningCredentials().Key.KeyId.ShouldBe("next");
        keyRing.GetValidationKeys("current").ShouldHaveSingleItem();
        keyRing.GetValidationKeys("next").ShouldHaveSingleItem();
        keyRing.GetValidationKeys("unknown").ShouldBeEmpty();
        time.GetUtcNow().ShouldBe(TestData.Now);
    }

    [Fact]
    public void GetValidationKeys_RetiredKey_NotReturned()
    {
        var (_, keyRing, _) = Create(extraKey: new JwtSigningKeyOptions
        {
            KeyId = "old",
            Key = NewKey(),
            RetireAfter = TestData.Now.AddMinutes(-1),
        });

        keyRing.GetValidationKeys("old").ShouldBeEmpty();
    }

    private static (JwtAccessTokenIssuer Issuer, JwtKeyRing KeyRing, FakeTimeProvider Time) Create(JwtSigningKeyOptions? extraKey = null)
    {
        var time = new FakeTimeProvider(TestData.Now);
        var settings = new JwtOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            AccessTokenLifetime = TimeSpan.FromMinutes(15),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        settings.SigningKeys.Add(new JwtSigningKeyOptions { KeyId = "current", Key = NewKey(), NotBefore = TestData.Now.AddDays(-30) });
        if (extraKey is not null)
        {
            settings.SigningKeys.Add(extraKey);
        }

        var options = Options.Create(settings);
        var ids = Substitute.For<IIdGenerator>();
        ids.NewId().Returns(_ => Guid.CreateVersion7(time.GetUtcNow()));

        var keyRing = new JwtKeyRing(options, time);
        return (new JwtAccessTokenIssuer(keyRing, options, ids, time), keyRing, time);
    }

    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
