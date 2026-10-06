using AutoMarket.BuildingBlocks.Application;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Application.Auth.ChangePassword;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.Domain.Users.Events;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Application.Auth.ChangePassword;

// FR-AUTH-07, SEC-AUTH-03 (yanlış cari şifrə lockout sayğacına yazılır)
public sealed class ChangePasswordHandlerTests
{
    private const string CurrentPassword = "current-Passw0rd!";
    private const string NewPassword = "brand-new-Passw0rd!";
    private const string RawRefreshToken = "current-session";

    private readonly AuthHandlerFixture _fixture = new();
    private readonly User _user = new UserBuilder().Build();

    public ChangePasswordHandlerTests()
    {
        _fixture.Users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _fixture.Passwords.Verify(_user, CurrentPassword).Returns(PasswordCheckResult.Success);
        _fixture.Passwords.Verify(_user, Arg.Is<string>(password => password != CurrentPassword)).Returns(PasswordCheckResult.Failed);
    }

    [Fact]
    public async Task Handle_CurrentSessionCookie_RevokesOtherSessionsOnly()
    {
        var current = _fixture.GivenRefreshToken(_user.Id, RawRefreshToken);

        var result = await HandleAsync(CurrentPassword, RawRefreshToken);

        result.IsSuccess.ShouldBeTrue();
        _fixture.Passwords.Received(1).SetPassword(_user, NewPassword);
        await _fixture.RefreshTokens.Received(1).RevokeAllActiveExceptFamilyAsync(
            _user.Id, current.FamilyId, RefreshTokenRevocationReason.PasswordChanged, TestData.Now, Arg.Any<CancellationToken>());
        await _fixture.RefreshTokens.DidNotReceiveWithAnyArgs().RevokeAllActiveAsync(default, default, default, TestContext.Current.CancellationToken);
        _user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PasswordChangedDomainEvent>();
        _fixture.ShouldHaveAudited(AuthAuditEvents.PasswordChanged);
    }

    [Fact]
    public async Task Handle_NoCookie_RevokesAllSessions()
    {
        var result = await HandleAsync(CurrentPassword, refreshToken: null);

        result.IsSuccess.ShouldBeTrue();
        await _fixture.RefreshTokens.Received(1)
            .RevokeAllActiveAsync(_user.Id, RefreshTokenRevocationReason.PasswordChanged, TestData.Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CookieOfAnotherUser_RevokesAllSessions()
    {
        _fixture.GivenRefreshToken(Guid.CreateVersion7(TestData.Now), RawRefreshToken);

        await HandleAsync(CurrentPassword, RawRefreshToken);

        await _fixture.RefreshTokens.Received(1)
            .RevokeAllActiveAsync(_user.Id, RefreshTokenRevocationReason.PasswordChanged, TestData.Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_InvalidCredentialsAndCountsFailure()
    {
        var result = await HandleAsync("wrong-password", RawRefreshToken);

        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
        _user.AccessFailedCount.ShouldBe(1);
        _fixture.Passwords.DidNotReceiveWithAnyArgs().SetPassword(default!, default!);
        _fixture.ShouldHaveAudited(AuthAuditEvents.PasswordChangeFailed);
        await _fixture.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FifthWrongCurrentPassword_LocksOut()
    {
        for (var i = 0; i < TestData.Lockout.MaxFailedAttempts; i++)
        {
            await HandleAsync("wrong-password", null);
        }

        _user.IsLockedOut(TestData.Now).ShouldBeTrue();
        _fixture.ShouldHaveAudited(AuthAuditEvents.LockedOut);
    }

    [Fact]
    public async Task Handle_LockedOut_RejectedWithoutVerifyingPassword()
    {
        for (var i = 0; i < TestData.Lockout.MaxFailedAttempts; i++)
        {
            _user.RecordFailedLogin(TestData.Now, TestData.Lockout);
        }

        _fixture.Passwords.ClearReceivedCalls();

        var result = await HandleAsync(CurrentPassword, null);

        result.Error.ShouldBe(AuthErrors.AccountLockedOut);
        _fixture.Passwords.DidNotReceiveWithAnyArgs().Verify(default!, default!);
    }

    [Fact]
    public async Task Handle_NewPasswordContainsName_ValidationFailed()
    {
        _fixture.PasswordPolicy.Validate(NewPassword, _user.Email, _user.Name)
            .Returns([new PasswordPolicyViolation("PASSWORD_CONTAINS_PERSONAL_INFO", "Password must not contain your email or name.")]);

        var result = await HandleAsync(CurrentPassword, null);

        result.Error!.Code.ShouldBe(ErrorCodes.ValidationFailed);
        _fixture.Passwords.DidNotReceiveWithAnyArgs().SetPassword(default!, default!);
    }

    private Task<Result> HandleAsync(string currentPassword, string? refreshToken) =>
        new ChangePasswordHandler(
            _fixture.Users,
            _fixture.RefreshTokens,
            _fixture.Passwords,
            _fixture.PasswordPolicy,
            _fixture.UnitOfWork,
            _fixture.Audit,
            TestData.Options(),
            _fixture.Time).HandleAsync(
                new ChangePasswordCommand(_user.Id, currentPassword, NewPassword, refreshToken),
                TestContext.Current.CancellationToken);
}
