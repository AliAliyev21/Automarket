using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Application.Auth.ForgotPassword;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.Domain.Users.Events;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Application.Auth.ForgotPassword;

// FR-AUTH-06 AC1/AC2, SEC-AUTH-08, SEC-RATE-03
public sealed class ForgotPasswordHandlerTests
{
    private readonly AuthHandlerFixture _fixture = new();

    [Fact]
    public async Task Handle_ActiveUser_IssuesOneHourTokenRevokingPreviousAndRequestsEmail()
    {
        var user = GivenUser(new UserBuilder().Build());

        var result = await HandleAsync(user.Email!);

        result.IsSuccess.ShouldBeTrue();
        await _fixture.OneTimeTokens.Received(1)
            .RevokeActiveAsync(user.Id, OneTimeTokenPurpose.PasswordReset, TestData.Now, Arg.Any<CancellationToken>());
        _fixture.OneTimeTokens.Received(1).Add(Arg.Is<OneTimeToken>(token =>
            token.UserId == user.Id
            && token.Purpose == OneTimeTokenPurpose.PasswordReset
            && token.ExpiresAt == TestData.Now.AddHours(1)
            && token.TokenHash.SequenceEqual(TokenHasher.Hash("token-1"))));
        user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PasswordResetRequestedDomainEvent>().RawToken.ShouldBe("token-1");
        _fixture.ShouldHaveAudited(AuthAuditEvents.PasswordResetRequested);
        await _fixture.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownEmail_SameSuccessWithoutToken()
    {
        var result = await HandleAsync("missing@automarket.az");

        result.IsSuccess.ShouldBeTrue();
        _fixture.OneTimeTokens.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_BlockedUser_SameSuccessWithoutToken()
    {
        var user = GivenUser(new UserBuilder().Blocked().Build());

        var result = await HandleAsync(user.Email!);

        result.IsSuccess.ShouldBeTrue();
        _fixture.OneTimeTokens.DidNotReceiveWithAnyArgs().Add(default!);
        user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_EmailRateLimited_RateLimitedWithoutLookup()
    {
        _fixture.RateLimits.TryAcquireAsync(RateLimitRules.RecoveryEmail, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RateLimitDecision(false, TimeSpan.FromMinutes(10)));

        var result = await HandleAsync("user@automarket.az");

        result.Error!.Code.ShouldBe(ErrorCodes.RateLimited);
        await _fixture.Users.DidNotReceiveWithAnyArgs().FindByEmailAsync(default!, TestContext.Current.CancellationToken);
    }

    private User GivenUser(User user)
    {
        _fixture.Users.FindByEmailAsync(user.Email!, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private Task<Result> HandleAsync(string email) =>
        new ForgotPasswordHandler(
            _fixture.Users,
            _fixture.OneTimeTokens,
            _fixture.TokenGenerator,
            _fixture.UnitOfWork,
            _fixture.RateLimits,
            _fixture.Audit,
            _fixture.Ids,
            TestData.Options(),
            _fixture.Time).HandleAsync(new ForgotPasswordCommand(email), TestContext.Current.CancellationToken);
}
