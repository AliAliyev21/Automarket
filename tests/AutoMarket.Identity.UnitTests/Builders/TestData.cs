using AutoMarket.Identity.Application;
using AutoMarket.Identity.Domain.Users;
using Microsoft.Extensions.Options;

namespace AutoMarket.Identity.UnitTests.Builders;

// Testlərdə istifadə olunan sabit vaxt və konfiqurasiya (appsettings.json-dakı dəyərlərlə eyni, NFR-TEST-08)
internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 6, 9, 0, 0, TimeSpan.Zero);

    public static readonly LockoutPolicy Lockout = new(
        MaxFailedAttempts: 5,
        FailureWindow: TimeSpan.FromMinutes(15),
        BaseDuration: TimeSpan.FromMinutes(15),
        MaxDuration: TimeSpan.FromHours(24));

    public static IOptions<IdentityOptions> Options() => Microsoft.Extensions.Options.Options.Create(new IdentityOptions
    {
        Password = new IdentityOptions.PasswordSettings { IterationCount = 210_000, MinLength = 10, MaxLength = 128 },
        Lockout = new IdentityOptions.LockoutSettings
        {
            MaxFailedAttempts = Lockout.MaxFailedAttempts,
            FailureWindow = Lockout.FailureWindow,
            BaseDuration = Lockout.BaseDuration,
            MaxDuration = Lockout.MaxDuration,
        },
        Tokens = new IdentityOptions.TokenSettings { EmailConfirmationLifetime = TimeSpan.FromHours(24) },
        RefreshTokens = new IdentityOptions.RefreshTokenSettings
        {
            SlidingLifetime = TimeSpan.FromDays(14),
            AbsoluteLifetime = TimeSpan.FromDays(60),
            MaxSessions = 10,
        },
    });
}
