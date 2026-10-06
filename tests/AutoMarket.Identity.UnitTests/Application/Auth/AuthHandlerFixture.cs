using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.UnitTests.Builders;
using Microsoft.Extensions.Time.Testing;

namespace AutoMarket.Identity.UnitTests.Application.Auth;

// Handler testləri üçün portların fake-ləri (NSubstitute yalnız port interfeysləri üçün, CONVENTIONS §11.3)
internal sealed class AuthHandlerFixture
{
    private int _tokenCounter;

    public AuthHandlerFixture()
    {
        Ids.NewId().Returns(_ => Guid.CreateVersion7(Time.GetUtcNow()));
        TokenGenerator.Generate().Returns(_ => $"token-{Interlocked.Increment(ref _tokenCounter)}");
        RateLimits.TryAcquireAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(RateLimitDecision.Allowed);
        Users.GetRolesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([Roles.User]);
        RefreshTokens.GetActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
        AccessTokens.Issue(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<string>>())
            .Returns(_ => new AccessToken("access-token", Time.GetUtcNow().AddMinutes(15)));
        UnitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Substitute.For<IUnitOfWorkTransaction>());
        PasswordPolicy.Validate(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>()).Returns([]);
    }

    public FakeTimeProvider Time { get; } = new(TestData.Now);

    public IUserRepository Users { get; } = Substitute.For<IUserRepository>();

    public IRefreshTokenRepository RefreshTokens { get; } = Substitute.For<IRefreshTokenRepository>();

    public IOneTimeTokenRepository OneTimeTokens { get; } = Substitute.For<IOneTimeTokenRepository>();

    public IPasswordService Passwords { get; } = Substitute.For<IPasswordService>();

    public IAccessTokenIssuer AccessTokens { get; } = Substitute.For<IAccessTokenIssuer>();

    public IIdentityUnitOfWork UnitOfWork { get; } = Substitute.For<IIdentityUnitOfWork>();

    public IRateLimitService RateLimits { get; } = Substitute.For<IRateLimitService>();

    public IAuditLog Audit { get; } = Substitute.For<IAuditLog>();

    public IIdGenerator Ids { get; } = Substitute.For<IIdGenerator>();

    public ISecureTokenGenerator TokenGenerator { get; } = Substitute.For<ISecureTokenGenerator>();

    public IRequestContext RequestContext { get; } = Substitute.For<IRequestContext>();

    public IPasswordPolicy PasswordPolicy { get; } = Substitute.For<IPasswordPolicy>();

    public IUserStatusCache StatusCache { get; } = Substitute.For<IUserStatusCache>();

    public SessionService Sessions => new(RefreshTokens, Users, AccessTokens, TokenGenerator, Ids, RequestContext, TestData.Options());

    public EmailConfirmationIssuer EmailConfirmation => new(OneTimeTokens, TokenGenerator, Ids, TestData.Options());

    // Repository-də hash ilə tapılan aktiv refresh token (yeni ailə)
    public RefreshToken GivenRefreshToken(Guid userId, string rawToken)
    {
        var createdAt = TestData.Now.AddDays(-1);
        var token = RefreshToken.Issue(
            Guid.CreateVersion7(createdAt),
            userId,
            Guid.CreateVersion7(createdAt),
            null,
            TokenHasher.Hash(rawToken),
            createdAt,
            TimeSpan.FromDays(14),
            createdAt.AddDays(60),
            null,
            null);

        RefreshTokens.FindByHashAsync(Arg.Is<byte[]>(hash => hash.SequenceEqual(token.TokenHash)), Arg.Any<CancellationToken>())
            .Returns(token);
        return token;
    }

    public void ShouldHaveAudited(string eventType) =>
        Audit.Received().Record(Arg.Is<AuditEntry>(entry => entry.EventType == eventType));
}
