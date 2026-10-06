using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.RateLimiting;
using AutoMarket.BuildingBlocks.Web.Security;

namespace AutoMarket.BuildingBlocks.UnitTests.Web;

public sealed class OptionsValidatorTests
{
    // SEC-NET-03: wildcard və https olmayan origin qəbul edilmir
    [Theory]
    [InlineData("https://app.automarket.az", true)]
    [InlineData("https://localhost:5173", true)]
    [InlineData("http://app.automarket.az", false)]
    [InlineData("https://*.automarket.az", false)]
    [InlineData("*", false)]
    [InlineData("https://app.automarket.az/", false)]
    [InlineData("https://app.automarket.az/path", false)]
    public void AllowedOrigins_Origin_Validated(string origin, bool valid)
    {
        var options = new AllowedOriginsOptions();
        options.AllowedOrigins.Add(origin);

        new AllowedOriginsOptionsValidator().Validate(null, options).Succeeded.ShouldBe(valid);
    }

    [Fact]
    public void AllowedOrigins_IsAllowed_CaseInsensitiveExactMatch()
    {
        var options = new AllowedOriginsOptions();
        options.AllowedOrigins.Add("https://app.automarket.az");

        options.IsAllowed("https://APP.automarket.az").ShouldBeTrue();
        options.IsAllowed("https://evil.automarket.az").ShouldBeFalse();
        options.IsAllowed(null).ShouldBeFalse();
    }

    [Fact]
    public void RateLimiting_AllRulesConfigured_Succeeds() =>
        new RateLimitingOptionsValidator().Validate(null, Complete()).Succeeded.ShouldBeTrue();

    [Fact]
    public void RateLimiting_MissingRule_Fails()
    {
        var options = Complete();
        options.Rules.Remove(RateLimitRules.LoginIp);

        var result = new RateLimitingOptionsValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(RateLimitRules.LoginIp);
    }

    [Fact]
    public void RateLimiting_NonPositiveLimit_Fails()
    {
        var options = Complete();
        options.Rules[RateLimitRules.Register].PermitLimit = 0;

        new RateLimitingOptionsValidator().Validate(null, options).Failed.ShouldBeTrue();
    }

    private static RateLimitingOptions Complete()
    {
        var options = new RateLimitingOptions();
        foreach (var rule in RateLimitRules.All)
        {
            options.Rules[rule] = new RateLimitRule { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) };
        }

        return options;
    }
}
