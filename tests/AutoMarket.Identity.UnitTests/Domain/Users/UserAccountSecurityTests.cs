using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.Domain.Users.Events;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Domain.Users;

// FR-ADM-01 (bloklama), FR-AUTH-06/07 (şifrə), FR-ADM-02 (rollar)
public sealed class UserAccountSecurityTests
{
    private static readonly DateTimeOffset Now = TestData.Now;

    [Fact]
    public void Block_Active_BecomesBlockedAndRaisesEvent()
    {
        var user = new UserBuilder().Build();

        var changed = user.Block("Spam", Now);

        changed.ShouldBeTrue();
        user.Status.ShouldBe(UserStatus.Blocked);
        user.BlockedAt.ShouldBe(Now);
        user.BlockReason.ShouldBe("Spam");
        user.DomainEvents.ShouldHaveSingleItem().ShouldBe(new UserBlockedDomainEvent(user.Id, Now));
    }

    [Fact]
    public void Block_AlreadyBlocked_NoChangeNoEvent()
    {
        var user = new UserBuilder().Blocked().Build();
        var blockedAt = user.BlockedAt;

        var changed = user.Block("Other", Now);

        changed.ShouldBeFalse();
        user.BlockedAt.ShouldBe(blockedAt);
        user.BlockReason.ShouldBe("Spam");
        user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Block_EmptyReason_Throws() =>
        Should.Throw<ArgumentException>(() => new UserBuilder().Build().Block(" ", Now));

    [Fact]
    public void Unblock_ConfirmedUser_BecomesActive()
    {
        var user = new UserBuilder().Blocked().Build();

        var changed = user.Unblock();

        changed.ShouldBeTrue();
        user.Status.ShouldBe(UserStatus.Active);
        user.BlockedAt.ShouldBeNull();
        user.BlockReason.ShouldBeNull();
        user.DomainEvents.ShouldHaveSingleItem().ShouldBe(new UserUnblockedDomainEvent(user.Id));
    }

    [Fact]
    public void Unblock_UnconfirmedUser_ReturnsToUnconfirmed()
    {
        var user = new UserBuilder().Unconfirmed().Blocked().Build();

        user.Unblock();

        user.Status.ShouldBe(UserStatus.Unconfirmed);
    }

    [Fact]
    public void Unblock_NotBlocked_NoEvent()
    {
        var user = new UserBuilder().Build();

        user.Unblock().ShouldBeFalse();
        user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void CompletePasswordReset_LockedOut_ResetsLockoutAndRaisesPasswordChanged()
    {
        var user = new UserBuilder().Build();
        for (var i = 0; i < TestData.Lockout.MaxFailedAttempts; i++)
        {
            user.RecordFailedLogin(Now, TestData.Lockout);
        }

        user.ClearDomainEvents();

        user.CompletePasswordReset();

        user.IsLockedOut(Now).ShouldBeFalse();
        user.LockoutLevel.ShouldBe(0);
        user.AccessFailedCount.ShouldBe(0);
        user.DomainEvents.ShouldHaveSingleItem().ShouldBe(new PasswordChangedDomainEvent(user.Id, user.Email!));
    }

    [Fact]
    public void RequestPasswordReset_RaisesEventWithToken()
    {
        var user = new UserBuilder().Build();

        user.RequestPasswordReset("raw", Now.AddHours(1));

        user.DomainEvents.ShouldHaveSingleItem()
            .ShouldBe(new PasswordResetRequestedDomainEvent(user.Id, user.Email!, "raw", Now.AddHours(1)));
    }

    [Fact]
    public void RegisterConfirmed_IsActive()
    {
        var user = User.RegisterConfirmed(Guid.CreateVersion7(Now), "admin@automarket.az", "Admin", Now);

        user.Status.ShouldBe(UserStatus.Active);
        user.EmailConfirmed.ShouldBeTrue();
    }
}
