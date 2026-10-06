using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth;
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

    public SessionService Sessions => new(RefreshTokens, Users, AccessTokens, TokenGenerator, Ids, RequestContext, TestData.Options());

    public EmailConfirmationIssuer EmailConfirmation => new(OneTimeTokens, TokenGenerator, Ids, TestData.Options());

    public void ShouldHaveAudited(string eventType) =>
        Audit.Received().Record(Arg.Is<AuditEntry>(entry => entry.EventType == eventType));
}
