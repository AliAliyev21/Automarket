using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Application.Auth.ConfirmEmail;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Application.Auth.ConfirmEmail;

// FR-AUTH-02 AC1/AC2
public sealed class ConfirmEmailHandlerTests
{
    private const string RawToken = "confirmation-token";

    private readonly AuthHandlerFixture _fixture = new();
    private readonly User _user = new UserBuilder().Unconfirmed().Build();

    public ConfirmEmailHandlerTests() =>
        _fixture.Users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);

    [Fact]
    public async Task Handle_ValidToken_ConfirmsAndConsumesToken()
    {
        var token = GivenToken(TestData.Now.AddHours(1));

        var result = await HandleAsync();

        result.IsSuccess.ShouldBeTrue();
        _user.Status.ShouldBe(UserStatus.Active);
        token.UsedAt.ShouldBe(TestData.Now);
        _fixture.ShouldHaveAudited(AuthAuditEvents.EmailConfirmed);
    }

    [Fact]
    public async Task Handle_UnknownToken_TokenInvalidOrExpired() =>
        (await HandleAsync()).Error.ShouldBe(AuthErrors.TokenInvalidOrExpired);

    [Fact]
    public async Task Handle_ExpiredToken_TokenInvalidOrExpired()
    {
        GivenToken(TestData.Now);

        (await HandleAsync()).Error.ShouldBe(AuthErrors.TokenInvalidOrExpired);
        _user.Status.ShouldBe(UserStatus.Unconfirmed);
    }

    [Fact]
    public async Task Handle_UsedToken_TokenInvalidOrExpired()
    {
        GivenToken(TestData.Now.AddHours(1)).Use(TestData.Now.AddMinutes(-5));

        (await HandleAsync()).Error.ShouldBe(AuthErrors.TokenInvalidOrExpired);
    }

    [Fact]
    public async Task Handle_RevokedToken_TokenInvalidOrExpired()
    {
        GivenToken(TestData.Now.AddHours(1)).Revoke(TestData.Now.AddMinutes(-5));

        (await HandleAsync()).Error.ShouldBe(AuthErrors.TokenInvalidOrExpired);
    }

    private OneTimeToken GivenToken(DateTimeOffset expiresAt)
    {
        var token = OneTimeToken.Issue(
            Guid.CreateVersion7(TestData.Now),
            _user.Id,
            OneTimeTokenPurpose.EmailConfirmation,
            TokenHasher.Hash(RawToken),
            TestData.Now.AddHours(-23),
            expiresAt);

        _fixture.OneTimeTokens.FindByHashAsync(Arg.Is<byte[]>(hash => hash.SequenceEqual(token.TokenHash)), Arg.Any<CancellationToken>())
            .Returns(token);
        return token;
    }

    private Task<Result> HandleAsync() =>
        new ConfirmEmailHandler(_fixture.OneTimeTokens, _fixture.Users, _fixture.UnitOfWork, _fixture.Audit, _fixture.Time)
            .HandleAsync(new ConfirmEmailCommand(RawToken), TestContext.Current.CancellationToken);
}
