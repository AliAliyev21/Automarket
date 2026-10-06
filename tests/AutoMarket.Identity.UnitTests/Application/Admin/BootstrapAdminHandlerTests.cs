using AutoMarket.BuildingBlocks.Application;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Admin;
using AutoMarket.Identity.Application.Admin.BootstrapAdmin;
using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.Domain.Users.Events;
using AutoMarket.Identity.UnitTests.Application.Auth;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Application.Admin;

// ARCHITECTURE §11, R-05: ilk Admin yalnız aktiv Admin olmadıqda yaradılır
public sealed class BootstrapAdminHandlerTests
{
    private const string Email = "Admin@AutoMarket.az";
    private const string Password = "bootstrap-Passw0rd!";

    private readonly AuthHandlerFixture _fixture = new();

    [Fact]
    public async Task Handle_NoAdminNewEmail_CreatesConfirmedAdmin()
    {
        var result = await HandleAsync();

        result.Value.ShouldBe(BootstrapAdminOutcome.Created);
        await _fixture.Users.Received(1).CreateAsync(
            Arg.Is<User>(user => user.Email == "admin@automarket.az" && user.Status == UserStatus.Active),
            Password,
            Arg.Any<CancellationToken>());
        await _fixture.Users.Received(1).AddRoleAsync(Arg.Any<Guid>(), Roles.Admin, Arg.Any<CancellationToken>());
        _fixture.ShouldHaveAudited(AdminAuditEvents.AdminBootstrapped);
        await _fixture.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingActiveUser_PromotedWithoutChangingPassword()
    {
        var user = new UserBuilder().WithEmail("admin@automarket.az").Build();
        _fixture.Users.FindByEmailAsync("admin@automarket.az", Arg.Any<CancellationToken>()).Returns(user);

        var result = await HandleAsync();

        result.Value.ShouldBe(BootstrapAdminOutcome.Promoted);
        await _fixture.Users.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, TestContext.Current.CancellationToken);
        await _fixture.Users.Received(1).AddRoleAsync(user.Id, Roles.Admin, Arg.Any<CancellationToken>());
        user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserRolesChangedDomainEvent>().Roles.ShouldBe([Roles.Admin, Roles.User]);
    }

    [Fact]
    public async Task Handle_ActiveAdminExists_Forbidden()
    {
        _fixture.Users.AnyActiveAdminAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await HandleAsync();

        result.Error!.Code.ShouldBe(ErrorCodes.Forbidden);
        await _fixture.Users.DidNotReceiveWithAnyArgs().AddRoleAsync(default, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ExistingBlockedUser_Forbidden()
    {
        var user = new UserBuilder().WithEmail("admin@automarket.az").Blocked().Build();
        _fixture.Users.FindByEmailAsync("admin@automarket.az", Arg.Any<CancellationToken>()).Returns(user);

        var result = await HandleAsync();

        result.Error!.Code.ShouldBe(ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Handle_WeakPassword_ValidationFailed()
    {
        _fixture.PasswordPolicy.Validate(Password, Arg.Any<string?>(), Arg.Any<string?>())
            .Returns([new PasswordPolicyViolation("PASSWORD_TOO_COMMON", "Password is too common.")]);

        var result = await HandleAsync();

        result.Error!.Code.ShouldBe(ErrorCodes.ValidationFailed);
        await _fixture.Users.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ValidationFailed()
    {
        var result = await HandleAsync("not-an-email");

        result.Error!.Code.ShouldBe(ErrorCodes.ValidationFailed);
    }

    private Task<Result<BootstrapAdminOutcome>> HandleAsync(string email = Email) =>
        new BootstrapAdminHandler(_fixture.Users, _fixture.PasswordPolicy, _fixture.UnitOfWork, _fixture.Audit, _fixture.Ids, _fixture.Time)
            .HandleAsync(new BootstrapAdminCommand(email, Password, "Administrator"), TestContext.Current.CancellationToken);
}
