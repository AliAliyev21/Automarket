using AutoMarket.BuildingBlocks.Application;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Application.Auth.Logout;
using AutoMarket.Identity.Application.Auth.LogoutAll;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Application.Auth.Logout;

// FR-AUTH-05 AC1/AC2
public sealed class LogoutHandlerTests
{
    private const string RawToken = "current-refresh-token";

    private readonly AuthHandlerFixture _fixture = new();
    private readonly Guid _userId = Guid.CreateVersion7(TestData.Now);

    [Fact]
    public async Task Logout_KnownToken_RevokesItsFamilyAndAudits()
    {
        var token = _fixture.GivenRefreshToken(_userId, RawToken);

        var result = await LogoutAsync(RawToken);

        result.IsSuccess.ShouldBeTrue();
        await _fixture.RefreshTokens.Received(1)
            .RevokeFamilyAsync(token.FamilyId, RefreshTokenRevocationReason.Logout, TestData.Now, Arg.Any<CancellationToken>());
        _fixture.ShouldHaveAudited(AuthAuditEvents.Logout);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-token")]
    public async Task Logout_MissingOrUnknownToken_SucceedsWithoutRevoking(string? rawToken)
    {
        var result = await LogoutAsync(rawToken);

        result.IsSuccess.ShouldBeTrue();
        await _fixture.RefreshTokens.DidNotReceiveWithAnyArgs().RevokeFamilyAsync(default, default, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task LogoutAll_RevokesAllSessionsAndAudits()
    {
        var result = await new LogoutAllHandler(_fixture.RefreshTokens, _fixture.UnitOfWork, _fixture.Audit, _fixture.Time)
            .HandleAsync(new LogoutAllCommand(_userId), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        await _fixture.RefreshTokens.Received(1)
            .RevokeAllActiveAsync(_userId, RefreshTokenRevocationReason.LogoutAll, TestData.Now, Arg.Any<CancellationToken>());
        _fixture.ShouldHaveAudited(AuthAuditEvents.LogoutAll);
    }

    private Task<Result> LogoutAsync(string? rawToken) =>
        new LogoutHandler(_fixture.RefreshTokens, _fixture.UnitOfWork, _fixture.Audit, _fixture.Time)
            .HandleAsync(new LogoutCommand(rawToken), TestContext.Current.CancellationToken);
}
