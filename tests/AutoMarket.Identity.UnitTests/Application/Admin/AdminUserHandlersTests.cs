using AutoMarket.BuildingBlocks.Application;
using AutoMarket.Identity.Application.Admin;
using AutoMarket.Identity.Application.Admin.Users.BlockUser;
using AutoMarket.Identity.Application.Admin.Users.GrantRole;
using AutoMarket.Identity.Application.Admin.Users.RevokeRole;
using AutoMarket.Identity.Application.Admin.Users.UnblockUser;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.Domain.Users.Events;
using AutoMarket.Identity.UnitTests.Application.Auth;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Application.Admin;

// FR-ADM-01 (bloklama), FR-ADM-02 (rollar), R-05
public sealed class AdminUserHandlersTests
{
    private readonly AuthHandlerFixture _fixture = new();
    private readonly Guid _adminId = Guid.CreateVersion7(TestData.Now.AddDays(-10));
    private readonly User _user = new UserBuilder().Build();

    public AdminUserHandlersTests()
    {
        _fixture.Users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _fixture.Users.GetRolesAsync(_user.Id, Arg.Any<CancellationToken>()).Returns([Roles.User]);
        _fixture.Users.AddRoleAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _fixture.Users.RemoveRoleAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    [Fact]
    public async Task Block_ActiveUser_BlocksRevokesSessionsAuditsAndInvalidatesCacheAfterSave()
    {
        var result = await BlockAsync(_user.Id);

        result.IsSuccess.ShouldBeTrue();
        _user.Status.ShouldBe(UserStatus.Blocked);
        _user.BlockReason.ShouldBe("Fraud");
        _user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserBlockedDomainEvent>();
        await _fixture.RefreshTokens.Received(1)
            .RevokeAllActiveAsync(_user.Id, RefreshTokenRevocationReason.Blocked, TestData.Now, Arg.Any<CancellationToken>());
        _fixture.ShouldHaveAudited(AdminAuditEvents.UserBlocked);
        Received.InOrder(() =>
        {
            _fixture.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _fixture.StatusCache.InvalidateAsync(_user.Id, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Block_Self_Forbidden()
    {
        var result = await BlockAsync(_adminId);

        result.Error.ShouldBe(CommonErrors.Forbidden);
        await _fixture.Users.DidNotReceiveWithAnyArgs().GetByIdAsync(default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Block_UnknownUser_UserNotFound()
    {
        var result = await BlockAsync(Guid.CreateVersion7(TestData.Now));

        result.Error.ShouldBe(AdminErrors.UserNotFound);
    }

    [Fact]
    public async Task Block_AlreadyBlocked_IdempotentWithoutRevoking()
    {
        _user.Block("Spam", TestData.Now.AddHours(-1));
        _user.ClearDomainEvents();

        var result = await BlockAsync(_user.Id);

        result.IsSuccess.ShouldBeTrue();
        await _fixture.RefreshTokens.DidNotReceiveWithAnyArgs().RevokeAllActiveAsync(default, default, default, TestContext.Current.CancellationToken);
        _user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unblock_BlockedUser_RestoresAndInvalidatesCache()
    {
        _user.Block("Spam", TestData.Now.AddHours(-1));
        _user.ClearDomainEvents();

        var result = await new UnblockUserHandler(_fixture.Users, _fixture.StatusCache, _fixture.UnitOfWork, _fixture.Audit)
            .HandleAsync(new UnblockUserCommand(_adminId, _user.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _user.Status.ShouldBe(UserStatus.Active);
        _user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserUnblockedDomainEvent>();
        _fixture.ShouldHaveAudited(AdminAuditEvents.UserUnblocked);
        await _fixture.StatusCache.Received(1).InvalidateAsync(_user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantRole_NewRole_AddsAndRaisesRolesChanged()
    {
        var result = await GrantAsync(Roles.Moderator);

        result.IsSuccess.ShouldBeTrue();
        await _fixture.Users.Received(1).AddRoleAsync(_user.Id, Roles.Moderator, Arg.Any<CancellationToken>());
        _user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserRolesChangedDomainEvent>()
            .Roles.ShouldBe([Roles.Moderator, Roles.User]);
        _fixture.ShouldHaveAudited(AdminAuditEvents.RoleGranted);
        await _fixture.RefreshTokens.DidNotReceiveWithAnyArgs().RevokeAllActiveAsync(default, default, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GrantRole_AlreadyHasRole_IdempotentWithoutEvent()
    {
        _fixture.Users.AddRoleAsync(_user.Id, Roles.Moderator, Arg.Any<CancellationToken>()).Returns(false);

        var result = await GrantAsync(Roles.Moderator);

        result.IsSuccess.ShouldBeTrue();
        _user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task GrantRole_UnknownUser_UserNotFound()
    {
        var result = await new GrantRoleHandler(_fixture.Users, _fixture.StatusCache, _fixture.UnitOfWork, _fixture.Audit)
            .HandleAsync(new GrantRoleCommand(_adminId, Guid.CreateVersion7(TestData.Now), Roles.Admin), TestContext.Current.CancellationToken);

        result.Error.ShouldBe(AdminErrors.UserNotFound);
    }

    [Fact]
    public async Task RevokeRole_Downgrade_RevokesSessionsAndInvalidatesCache()
    {
        _fixture.Users.GetRolesAsync(_user.Id, Arg.Any<CancellationToken>()).Returns([Roles.Moderator, Roles.User]);

        var result = await RevokeAsync(_user.Id, Roles.Moderator);

        result.IsSuccess.ShouldBeTrue();
        _user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserRolesChangedDomainEvent>().Roles.ShouldBe([Roles.User]);
        await _fixture.RefreshTokens.Received(1)
            .RevokeAllActiveAsync(_user.Id, RefreshTokenRevocationReason.RoleDowngraded, TestData.Now, Arg.Any<CancellationToken>());
        _fixture.ShouldHaveAudited(AdminAuditEvents.RoleRevoked);
        await _fixture.StatusCache.Received(1).InvalidateAsync(_user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeRole_OwnAdminRole_Forbidden()
    {
        var result = await RevokeAsync(_adminId, Roles.Admin);

        result.Error.ShouldBe(CommonErrors.Forbidden);
        await _fixture.Users.DidNotReceiveWithAnyArgs().RemoveRoleAsync(default, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RevokeRole_RoleNotAssigned_IdempotentWithoutRevoking()
    {
        _fixture.Users.RemoveRoleAsync(_user.Id, Roles.Admin, Arg.Any<CancellationToken>()).Returns(false);

        var result = await RevokeAsync(_user.Id, Roles.Admin);

        result.IsSuccess.ShouldBeTrue();
        await _fixture.RefreshTokens.DidNotReceiveWithAnyArgs().RevokeAllActiveAsync(default, default, default, TestContext.Current.CancellationToken);
    }

    private Task<Result> BlockAsync(Guid userId) =>
        new BlockUserHandler(_fixture.Users, _fixture.RefreshTokens, _fixture.StatusCache, _fixture.UnitOfWork, _fixture.Audit, _fixture.Time)
            .HandleAsync(new BlockUserCommand(_adminId, userId, "Fraud"), TestContext.Current.CancellationToken);

    private Task<Result> GrantAsync(string role) =>
        new GrantRoleHandler(_fixture.Users, _fixture.StatusCache, _fixture.UnitOfWork, _fixture.Audit)
            .HandleAsync(new GrantRoleCommand(_adminId, _user.Id, role), TestContext.Current.CancellationToken);

    private Task<Result> RevokeAsync(Guid userId, string role) =>
        new RevokeRoleHandler(_fixture.Users, _fixture.RefreshTokens, _fixture.StatusCache, _fixture.UnitOfWork, _fixture.Audit, _fixture.Time)
            .HandleAsync(new RevokeRoleCommand(_adminId, userId, role), TestContext.Current.CancellationToken);
}
