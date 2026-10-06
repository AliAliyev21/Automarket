using System.Net;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests.Security;

// SEC-RATE-01/02, ADR-0011: 429 + RATE_LIMITED + Retry-After, limitlər bütün instansiyalar üzrə ortaqdır (Redis)
public sealed class RateLimitTests(ContainersFixture containers)
{
    [Fact]
    public async Task Login_IpLimitExceeded_Returns429WithRetryAfter()
    {
        await using var factory = containers.CreateFactory(Limits(keyPrefix: Prefix(), ("login-ip", 2)));
        using var client = factory.CreateApiClient();

        for (var i = 0; i < 2; i++)
        {
            using var allowed = await AuthApi.LoginAsync(client, AuthApi.NewEmail());
            allowed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using var limited = await AuthApi.LoginAsync(client, AuthApi.NewEmail());

        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await AuthApi.ReadProblemAsync(limited)).Code.ShouldBe("RATE_LIMITED");
        int.Parse(limited.Headers.GetValues("Retry-After").Single(), System.Globalization.CultureInfo.InvariantCulture).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Login_EmailLimitExceeded_Returns429()
    {
        await using var factory = containers.CreateFactory(Limits(keyPrefix: Prefix(), ("login-email", 2)));
        using var client = factory.CreateApiClient();
        var email = AuthApi.NewEmail();

        (await AuthApi.LoginAsync(client, email)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.LoginAsync(client, email)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        using var limited = await AuthApi.LoginAsync(client, email);
        using var otherEmail = await AuthApi.LoginAsync(client, AuthApi.NewEmail());

        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Headers.Contains("Retry-After").ShouldBeTrue();
        otherEmail.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_LimitSharedAcrossInstances_SecondInstanceRejected()
    {
        // İki "instansiya" eyni Redis açarlarını paylaşır (NFR-TEST-04)
        var settings = Limits(keyPrefix: Prefix(), ("register", 2));
        await using var first = containers.CreateFactory(settings);
        await using var second = containers.CreateFactory(settings);
        using var firstClient = first.CreateApiClient();
        using var secondClient = second.CreateApiClient();

        (await AuthApi.RegisterAsync(firstClient, AuthApi.NewEmail())).StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await AuthApi.RegisterAsync(secondClient, AuthApi.NewEmail())).StatusCode.ShouldBe(HttpStatusCode.Accepted);
        using var limited = await AuthApi.RegisterAsync(firstClient, AuthApi.NewEmail());

        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    private static string Prefix() => $"rl-{Guid.CreateVersion7():N}";

    private static Dictionary<string, string?> Limits(string keyPrefix, params (string Rule, int Limit)[] rules)
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal) { ["RateLimiting:KeyPrefix"] = keyPrefix };
        foreach (var (rule, limit) in rules)
        {
            settings[$"RateLimiting:Rules:{rule}:PermitLimit"] = limit.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return settings;
    }
}
