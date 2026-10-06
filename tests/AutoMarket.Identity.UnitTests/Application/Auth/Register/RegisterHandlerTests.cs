using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Application.Auth.Register;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.Domain.Users.Events;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Application.Auth.Register;

// FR-AUTH-01, SEC-AUTH-08
public sealed class RegisterHandlerTests
{
    private const string Password = "x7!kq2#z9-strong";

    private readonly AuthHandlerFixture _fixture = new();

    [Fact]
    public async Task Handle_NewEmail_CreatesUnconfirmedUserWithConfirmationToken()
    {
        User? created = null;
        await _fixture.Users.CreateAsync(Arg.Do<User>(user => created = user), Password, Arg.Any<CancellationToken>());

        var result = await HandleAsync("  New.User@AutoMarket.AZ ");

        result.IsSuccess.ShouldBeTrue();
        created.ShouldNotBeNull();
        created.Email.ShouldBe("new.user@automarket.az");
        created.Status.ShouldBe(UserStatus.Unconfirmed);
        created.DomainEvents.OfType<EmailConfirmationRequestedDomainEvent>().ShouldHaveSingleItem().ExpiresAt.ShouldBe(TestData.Now.AddHours(24));
        _fixture.OneTimeTokens.Received(1).Add(Arg.Is<OneTimeToken>(token =>
            token.UserId == created.Id && token.Purpose == OneTimeTokenPurpose.EmailConfirmation));
        _fixture.ShouldHaveAudited(AuthAuditEvents.UserRegistered);
        await _fixture.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingEmail_SameResultAndWarningEmailWithoutNewUser()
    {
        var existing = new UserBuilder().WithEmail("taken@automarket.az").Build();
        _fixture.Users.FindByEmailAsync("taken@automarket.az", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await HandleAsync("taken@automarket.az");

        // FR-AUTH-01 AC4: cavab uğurlu qeydiyyatla eynidir, hash müddəti bərabərləşdirilir
        result.IsSuccess.ShouldBeTrue();
        _fixture.Passwords.Received(1).SimulateVerification(Password);
        await _fixture.Users.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, TestContext.Current.CancellationToken);
        existing.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<RegistrationAttemptedOnExistingAccountDomainEvent>();
        _fixture.ShouldHaveAudited(AuthAuditEvents.RegistrationAttemptOnExistingAccount);
    }

    private Task<BuildingBlocks.Application.Result> HandleAsync(string email) =>
        new RegisterHandler(
            _fixture.Users,
            _fixture.Passwords,
            _fixture.EmailConfirmation,
            _fixture.UnitOfWork,
            _fixture.Audit,
            _fixture.Ids,
            _fixture.Time).HandleAsync(new RegisterCommand(email, Password, "Leyla", null), TestContext.Current.CancellationToken);
}
