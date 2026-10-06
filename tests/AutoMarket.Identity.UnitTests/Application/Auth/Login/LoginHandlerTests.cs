using AutoMarket.BuildingBlocks.Application;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Application.Auth.Login;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Application.Auth.Login;

// FR-AUTH-03, SEC-AUTH-03, SEC-AUTH-08
public sealed class LoginHandlerTests
{
    private const string Password = "x7!kq2#z9-strong";

    private readonly AuthHandlerFixture _fixture = new();

    [Fact]
    public async Task Handle_EmailRateLimited_ReturnsRateLimitedWithoutLookup()
    {
        _fixture.RateLimits.TryAcquireAsync(RateLimitRules.LoginEmail, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RateLimitDecision(false, TimeSpan.FromMinutes(3)));

        var result = await HandleAsync("user@automarket.az");

        result.Error!.Code.ShouldBe(ErrorCodes.RateLimited);
        result.Error.RetryAfter.ShouldBe(TimeSpan.FromMinutes(3));
        await _fixture.Users.DidNotReceiveWithAnyArgs().FindByEmailAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_UnknownEmail_InvalidCredentialsWithDummyHash()
    {
        var result = await HandleAsync("missing@automarket.az");

        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
        _fixture.Passwords.Received(1).SimulateVerification(Password);
        _fixture.ShouldHaveAudited(AuthAuditEvents.LoginFailed);
    }

    [Fact]
    public async Task Handle_WrongPassword_InvalidCredentialsAndCountsFailure()
    {
        var user = GivenUser(new UserBuilder().Build(), PasswordCheckResult.Failed);

        var result = await HandleAsync(user.Email!);

        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
        user.AccessFailedCount.ShouldBe(1);
        await _fixture.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FifthWrongPassword_LocksOutAndStillInvalidCredentials()
    {
        var user = GivenUser(new UserBuilder().Build(), PasswordCheckResult.Failed);
        for (var i = 0; i < 4; i++)
        {
            await HandleAsync(user.Email!);
        }

        var result = await HandleAsync(user.Email!);

        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
        user.IsLockedOut(_fixture.Time.GetUtcNow()).ShouldBeTrue();
        _fixture.ShouldHaveAudited(AuthAuditEvents.LockedOut);
    }

    [Fact]
    public async Task Handle_CorrectPasswordWhileLockedOut_AccountLockedOut()
    {
        var user = LockedOutUser();
        _fixture.Passwords.Verify(user, Password).Returns(PasswordCheckResult.Success);

        var result = await HandleAsync(user.Email!);

        result.Error.ShouldBe(AuthErrors.AccountLockedOut);
    }

    [Fact]
    public async Task Handle_WrongPasswordWhileLockedOut_InvalidCredentialsAndLockNotExtended()
    {
        var user = LockedOutUser();
        var lockoutEnd = user.LockoutEnd;
        _fixture.Passwords.Verify(user, Password).Returns(PasswordCheckResult.Failed);

        var result = await HandleAsync(user.Email!);

        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
        user.LockoutEnd.ShouldBe(lockoutEnd);
        user.AccessFailedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_UnconfirmedWithCorrectPassword_EmailNotConfirmed()
    {
        var user = GivenUser(new UserBuilder().Unconfirmed().Build(), PasswordCheckResult.Success);

        var result = await HandleAsync(user.Email!);

        result.Error.ShouldBe(AuthErrors.EmailNotConfirmed);
        _fixture.RefreshTokens.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_UnconfirmedWithWrongPassword_InvalidCredentials()
    {
        // FR-AUTH-03 AC3: EMAIL_NOT_CONFIRMED yalnız şifrə düzgün olduqda
        var user = GivenUser(new UserBuilder().Unconfirmed().Build(), PasswordCheckResult.Failed);

        var result = await HandleAsync(user.Email!);

        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_CorrectPassword_ReturnsTokensAndResetsCounter()
    {
        var user = GivenUser(new UserBuilder().Build(), PasswordCheckResult.Success);
        user.RecordFailedLogin(_fixture.Time.GetUtcNow(), TestData.Lockout);

        var result = await HandleAsync(user.Email!);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        result.Value.AccessTokenExpiresInSeconds.ShouldBe(900);
        result.Value.RefreshTokenExpiresAt.ShouldBe(TestData.Now.AddDays(14));
        user.AccessFailedCount.ShouldBe(0);
        _fixture.RefreshTokens.Received(1).Add(Arg.Is<RefreshToken>(token => token.UserId == user.Id && token.ParentId == null));
        _fixture.ShouldHaveAudited(AuthAuditEvents.LoginSucceeded);
    }

    [Fact]
    public async Task Handle_RehashNeeded_Rehashes()
    {
        var user = GivenUser(new UserBuilder().Build(), PasswordCheckResult.SuccessRehashNeeded);

        await HandleAsync(user.Email!);

        _fixture.Passwords.Received(1).Rehash(user, Password);
    }

    [Fact]
    public async Task Handle_TenActiveSessions_RevokesOldest()
    {
        // SEC-AUTH-05: ən çox 10 aktiv sessiya, ən köhnəsi avtomatik ləğv olunur
        var user = GivenUser(new UserBuilder().Build(), PasswordCheckResult.Success);
        var active = Enumerable.Range(0, 10).Select(i => ActiveToken(user.Id, TestData.Now.AddDays(-10 + i))).ToList();
        _fixture.RefreshTokens.GetActiveForUserAsync(user.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(active);

        await HandleAsync(user.Email!);

        active[0].RevokedReason.ShouldBe(RefreshTokenRevocationReason.SessionLimit);
        active.Skip(1).ShouldAllBe(token => token.RevokedAt == null);
    }

    private User GivenUser(User user, PasswordCheckResult check)
    {
        _fixture.Users.FindByEmailAsync(user.Email!, Arg.Any<CancellationToken>()).Returns(user);
        _fixture.Passwords.Verify(user, Password).Returns(check);
        return user;
    }

    private User LockedOutUser()
    {
        var user = new UserBuilder().Build();
        for (var i = 0; i < 5; i++)
        {
            user.RecordFailedLogin(_fixture.Time.GetUtcNow(), TestData.Lockout);
        }

        user.ClearDomainEvents();
        _fixture.Users.FindByEmailAsync(user.Email!, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private static RefreshToken ActiveToken(Guid userId, DateTimeOffset createdAt) => RefreshToken.Issue(
        Guid.CreateVersion7(createdAt), userId, Guid.CreateVersion7(createdAt), null, new byte[32], createdAt,
        TimeSpan.FromDays(14), createdAt.AddDays(60), null, null);

    private Task<Result<SessionTokens>> HandleAsync(string email) =>
        new LoginHandler(
            _fixture.Users,
            _fixture.Passwords,
            _fixture.Sessions,
            _fixture.UnitOfWork,
            _fixture.RateLimits,
            _fixture.Audit,
            TestData.Options(),
            _fixture.Time).HandleAsync(new LoginCommand(email, Password), TestContext.Current.CancellationToken);
}
