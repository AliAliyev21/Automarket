using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Application.Auth.RefreshSession;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Application.Auth.RefreshSession;

// FR-AUTH-04: rotation, reuse detection, sliding/absolute müddət
public sealed class RefreshSessionHandlerTests
{
    private const string RawToken = "presented-refresh-token";

    private readonly AuthHandlerFixture _fixture = new();
    private readonly User _user = new UserBuilder().Build();

    public RefreshSessionHandlerTests()
    {
        _fixture.Users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _fixture.RefreshTokens.TryRevokeForRotationAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    [Fact]
    public async Task Handle_UnknownToken_Unauthorized()
    {
        var result = await HandleAsync();

        result.Error.ShouldBe(CommonErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_ActiveToken_RotatesInSameFamilyKeepingAbsoluteExpiry()
    {
        var current = GivenToken(TestData.Now.AddDays(-1), absoluteExpiresAt: TestData.Now.AddDays(40));

        var result = await HandleAsync();

        result.IsSuccess.ShouldBeTrue();
        await _fixture.RefreshTokens.Received(1).TryRevokeForRotationAsync(current.Id, Arg.Any<Guid>(), TestData.Now, Arg.Any<CancellationToken>());
        _fixture.RefreshTokens.Received(1).Add(Arg.Is<RefreshToken>(token =>
            token.FamilyId == current.FamilyId
            && token.ParentId == current.Id
            && token.AbsoluteExpiresAt == current.AbsoluteExpiresAt
            && token.ExpiresAt == TestData.Now.AddDays(14)));
    }

    [Fact]
    public async Task Handle_NearAbsoluteExpiry_NewTokenExpiresAtAbsolute()
    {
        var current = GivenToken(TestData.Now.AddDays(-1), absoluteExpiresAt: TestData.Now.AddDays(5));

        var result = await HandleAsync();

        result.Value.RefreshTokenExpiresAt.ShouldBe(current.AbsoluteExpiresAt);
    }

    [Fact]
    public async Task Handle_RotatedTokenPresentedAgain_RevokesAllSessions()
    {
        var current = GivenToken(TestData.Now.AddDays(-1));
        current.Revoke(TestData.Now.AddMinutes(-1), RefreshTokenRevocationReason.Rotated);

        var result = await HandleAsync();

        result.Error.ShouldBe(AuthErrors.RefreshTokenReused);
        await _fixture.RefreshTokens.Received(1)
            .RevokeAllActiveAsync(_user.Id, RefreshTokenRevocationReason.ReuseDetected, TestData.Now, Arg.Any<CancellationToken>());
        _fixture.ShouldHaveAudited(AuthAuditEvents.RefreshTokenReused);
        _fixture.RefreshTokens.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_ConcurrentRotationLost_TreatedAsReuse()
    {
        // ARCHITECTURE §13 A6: paralel ikinci refresh reuse sayılır
        GivenToken(TestData.Now.AddDays(-1));
        _fixture.RefreshTokens.TryRevokeForRotationAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await HandleAsync();

        result.Error.ShouldBe(AuthErrors.RefreshTokenReused);
    }

    [Fact]
    public async Task Handle_RevokedForOtherReason_UnauthorizedWithoutRevokingAll()
    {
        var current = GivenToken(TestData.Now.AddDays(-1));
        current.Revoke(TestData.Now.AddMinutes(-1), RefreshTokenRevocationReason.SessionLimit);

        var result = await HandleAsync();

        result.Error.ShouldBe(CommonErrors.Unauthorized);
        await _fixture.RefreshTokens.DidNotReceiveWithAnyArgs().RevokeAllActiveAsync(default, default, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_SlidingExpired_Unauthorized()
    {
        GivenToken(TestData.Now.AddDays(-15));

        var result = await HandleAsync();

        result.Error.ShouldBe(CommonErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_UserNotConfirmed_Unauthorized()
    {
        GivenToken(TestData.Now.AddDays(-1));
        _fixture.Users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(new UserBuilder().WithId(_user.Id).Unconfirmed().Build());

        var result = await HandleAsync();

        result.Error.ShouldBe(CommonErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_UserRateLimited_RateLimited()
    {
        GivenToken(TestData.Now.AddDays(-1));
        _fixture.RateLimits.TryAcquireAsync(RateLimitRules.Refresh, RateLimitKeys.ForUser(_user.Id), Arg.Any<CancellationToken>())
            .Returns(new RateLimitDecision(false, TimeSpan.FromSeconds(20)));

        var result = await HandleAsync();

        result.Error!.Code.ShouldBe(ErrorCodes.RateLimited);
    }

    private RefreshToken GivenToken(DateTimeOffset createdAt, DateTimeOffset? absoluteExpiresAt = null)
    {
        var token = RefreshToken.Issue(
            Guid.CreateVersion7(createdAt),
            _user.Id,
            Guid.CreateVersion7(createdAt),
            null,
            TokenHasher.Hash(RawToken),
            createdAt,
            TimeSpan.FromDays(14),
            absoluteExpiresAt ?? createdAt.AddDays(60),
            null,
            null);

        _fixture.RefreshTokens.FindByHashAsync(Arg.Is<byte[]>(hash => hash.SequenceEqual(token.TokenHash)), Arg.Any<CancellationToken>())
            .Returns(token);
        return token;
    }

    private Task<Result<SessionTokens>> HandleAsync() =>
        new RefreshSessionHandler(
            _fixture.RefreshTokens,
            _fixture.Users,
            _fixture.Sessions,
            _fixture.UnitOfWork,
            _fixture.RateLimits,
            _fixture.Audit,
            _fixture.Ids,
            _fixture.Time).HandleAsync(new RefreshSessionCommand(RawToken), TestContext.Current.CancellationToken);
}
