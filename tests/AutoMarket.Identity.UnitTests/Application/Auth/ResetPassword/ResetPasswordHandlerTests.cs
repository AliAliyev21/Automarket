using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Application.Auth.ResetPassword;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.Domain.Users.Events;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Application.Auth.ResetPassword;

// FR-AUTH-06 AC2–AC4, SEC-AUTH-07
public sealed class ResetPasswordHandlerTests
{
    private const string RawToken = "reset-token";
    private const string NewPassword = "brand-new-Passw0rd!";

    private readonly AuthHandlerFixture _fixture = new();
    private readonly User _user = new UserBuilder().Build();

    public ResetPasswordHandlerTests() =>
        _fixture.Users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);

    [Fact]
    public async Task Handle_ValidToken_SetsPasswordRevokesAllSessionsAndUsesToken()
    {
        var token = GivenToken(OneTimeTokenPurpose.PasswordReset, TestData.Now.AddMinutes(-10));
        for (var i = 0; i < TestData.Lockout.MaxFailedAttempts; i++)
        {
            _user.RecordFailedLogin(TestData.Now.AddMinutes(-5), TestData.Lockout);
        }

        _user.ClearDomainEvents();

        var result = await HandleAsync();

        result.IsSuccess.ShouldBeTrue();
        token.UsedAt.ShouldBe(TestData.Now);
        _fixture.Passwords.Received(1).SetPassword(_user, NewPassword);
        _user.IsLockedOut(TestData.Now).ShouldBeFalse();
        _user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PasswordChangedDomainEvent>();
        await _fixture.RefreshTokens.Received(1)
            .RevokeAllActiveAsync(_user.Id, RefreshTokenRevocationReason.PasswordReset, TestData.Now, Arg.Any<CancellationToken>());
        _fixture.ShouldHaveAudited(AuthAuditEvents.PasswordResetCompleted);
    }

    [Fact]
    public async Task Handle_UnknownToken_TokenInvalid()
    {
        var result = await HandleAsync();

        result.Error.ShouldBe(AuthErrors.TokenInvalidOrExpired);
    }

    [Fact]
    public async Task Handle_ExpiredToken_TokenInvalid()
    {
        GivenToken(OneTimeTokenPurpose.PasswordReset, TestData.Now.AddHours(-2));

        var result = await HandleAsync();

        result.Error.ShouldBe(AuthErrors.TokenInvalidOrExpired);
        _fixture.Passwords.DidNotReceiveWithAnyArgs().SetPassword(default!, default!);
    }

    [Fact]
    public async Task Handle_UsedToken_TokenInvalid()
    {
        var token = GivenToken(OneTimeTokenPurpose.PasswordReset, TestData.Now.AddMinutes(-10));
        token.Use(TestData.Now.AddMinutes(-5));

        var result = await HandleAsync();

        result.Error.ShouldBe(AuthErrors.TokenInvalidOrExpired);
    }

    [Fact]
    public async Task Handle_EmailConfirmationToken_TokenInvalid()
    {
        GivenToken(OneTimeTokenPurpose.EmailConfirmation, TestData.Now.AddMinutes(-10));

        var result = await HandleAsync();

        result.Error.ShouldBe(AuthErrors.TokenInvalidOrExpired);
    }

    [Fact]
    public async Task Handle_BlockedUser_TokenInvalid()
    {
        GivenToken(OneTimeTokenPurpose.PasswordReset, TestData.Now.AddMinutes(-10));
        _user.Block("Spam", TestData.Now.AddMinutes(-1));

        var result = await HandleAsync();

        result.Error.ShouldBe(AuthErrors.TokenInvalidOrExpired);
    }

    [Fact]
    public async Task Handle_PasswordContainsPersonalInfo_ValidationFailedWithFieldError()
    {
        var token = GivenToken(OneTimeTokenPurpose.PasswordReset, TestData.Now.AddMinutes(-10));
        _fixture.PasswordPolicy.Validate(NewPassword, _user.Email, _user.Name)
            .Returns([new PasswordPolicyViolation("PASSWORD_CONTAINS_PERSONAL_INFO", "Password must not contain your email or name.")]);

        var result = await HandleAsync();

        result.Error!.Code.ShouldBe(ErrorCodes.ValidationFailed);
        result.Error.FieldErrors.ShouldNotBeNull()[PasswordPolicyErrors.NewPasswordField]
            .ShouldHaveSingleItem().Code.ShouldBe("PASSWORD_CONTAINS_PERSONAL_INFO");
        token.UsedAt.ShouldBeNull();
    }

    private OneTimeToken GivenToken(OneTimeTokenPurpose purpose, DateTimeOffset issuedAt)
    {
        var token = OneTimeToken.Issue(
            Guid.CreateVersion7(issuedAt),
            _user.Id,
            purpose,
            TokenHasher.Hash(RawToken),
            issuedAt,
            issuedAt.AddHours(1));
        _fixture.OneTimeTokens.FindByHashAsync(Arg.Is<byte[]>(hash => hash.SequenceEqual(token.TokenHash)), Arg.Any<CancellationToken>())
            .Returns(token);
        return token;
    }

    private Task<Result> HandleAsync() =>
        new ResetPasswordHandler(
            _fixture.OneTimeTokens,
            _fixture.Users,
            _fixture.RefreshTokens,
            _fixture.Passwords,
            _fixture.PasswordPolicy,
            _fixture.UnitOfWork,
            _fixture.Audit,
            _fixture.Time).HandleAsync(new ResetPasswordCommand(RawToken, NewPassword), TestContext.Current.CancellationToken);
}
