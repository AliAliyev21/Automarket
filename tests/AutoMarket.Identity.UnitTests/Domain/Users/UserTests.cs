using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.Domain.Users.Events;
using AutoMarket.Identity.UnitTests.Builders;

namespace AutoMarket.Identity.UnitTests.Domain.Users;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = TestData.Now;

    [Fact]
    public void Register_NewUser_IsUnconfirmedAndRaisesRegistered()
    {
        var id = Guid.CreateVersion7(Now);

        var user = User.Register(id, "new@automarket.az", "Leyla", "+994501234567", Now);

        user.Status.ShouldBe(UserStatus.Unconfirmed);
        user.EmailConfirmed.ShouldBeFalse();
        user.Email.ShouldBe("new@automarket.az");
        user.UserName.ShouldBe("new@automarket.az");
        user.LockoutEnabled.ShouldBeFalse();
        user.DomainEvents.ShouldHaveSingleItem().ShouldBe(new UserRegisteredDomainEvent(id, Now));
    }

    [Fact]
    public void ConfirmEmail_Unconfirmed_BecomesActive()
    {
        var user = new UserBuilder().Unconfirmed().Build();

        user.ConfirmEmail(Now);

        user.Status.ShouldBe(UserStatus.Active);
        user.EmailConfirmed.ShouldBeTrue();
        user.ConfirmedAt.ShouldBe(Now);
        user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserEmailConfirmedDomainEvent>();
    }

    [Fact]
    public void ConfirmEmail_AlreadyActive_NoEvent()
    {
        var user = new UserBuilder().Build();

        user.ConfirmEmail(Now);

        user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RequestEmailConfirmation_Always_RaisesEventWithToken()
    {
        var user = new UserBuilder().Unconfirmed().Build();

        user.RequestEmailConfirmation("raw-token", Now.AddHours(24));

        var domainEvent = user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<EmailConfirmationRequestedDomainEvent>();
        domainEvent.RawToken.ShouldBe("raw-token");
        domainEvent.Email.ShouldBe(user.Email);
    }

    [Fact]
    public void RecordFailedLogin_FourAttempts_NotLockedOut()
    {
        var user = new UserBuilder().Build();

        for (var i = 0; i < 4; i++)
        {
            user.RecordFailedLogin(Now.AddMinutes(i), TestData.Lockout).ShouldBeFalse();
        }

        user.IsLockedOut(Now.AddMinutes(4)).ShouldBeFalse();
        user.AccessFailedCount.ShouldBe(4);
    }

    [Fact]
    public void RecordFailedLogin_FifthAttemptWithinWindow_LocksFor15Minutes()
    {
        var user = new UserBuilder().Build();

        var lockedOut = FailTimes(user, 5, Now);

        lockedOut.ShouldBeTrue();
        user.LockoutLevel.ShouldBe(1);
        user.LockoutEnd.ShouldBe(Now.AddMinutes(4) + TimeSpan.FromMinutes(15));
        user.IsLockedOut(Now.AddMinutes(5)).ShouldBeTrue();
        user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserLockedOutDomainEvent>();
    }

    [Fact]
    public void RecordFailedLogin_GapLongerThanWindow_CounterRestarts()
    {
        var user = new UserBuilder().Build();
        FailTimes(user, 4, Now);

        // SEC-AUTH-03: son uğursuz cəhddən 15 dəqiqədən çox keçib — sayğac yenidən başlayır
        var lockedOut = user.RecordFailedLogin(Now.AddMinutes(3) + TimeSpan.FromMinutes(16), TestData.Lockout);

        lockedOut.ShouldBeFalse();
        user.AccessFailedCount.ShouldBe(1);
    }

    [Fact]
    public void RecordFailedLogin_SecondLockout_DurationDoubles()
    {
        var user = new UserBuilder().Build();
        FailTimes(user, 5, Now);

        var afterFirstLockout = user.LockoutEnd!.Value.AddMinutes(1);
        FailTimes(user, 5, afterFirstLockout);

        user.LockoutLevel.ShouldBe(2);
        user.LockoutEnd.ShouldBe(afterFirstLockout.AddMinutes(4) + TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void RecordFailedLogin_ManyLockouts_CappedAt24Hours()
    {
        var user = new UserBuilder().Build();
        var batchStart = Now;

        for (var lockout = 0; lockout < 10; lockout++)
        {
            batchStart = lockout == 0 ? Now : user.LockoutEnd!.Value.AddMinutes(1);
            FailTimes(user, 5, batchStart);
        }

        user.LockoutLevel.ShouldBe(10);
        user.LockoutEnd.ShouldBe(batchStart.AddMinutes(4) + TimeSpan.FromHours(24));
    }

    [Fact]
    public void RecordSuccessfulLogin_AfterFailures_ResetsCounterAndLevel()
    {
        var user = new UserBuilder().Build();
        FailTimes(user, 5, Now);

        user.RecordSuccessfulLogin();

        user.AccessFailedCount.ShouldBe(0);
        user.LockoutLevel.ShouldBe(0);
        user.LockoutEnd.ShouldBeNull();
        user.LastFailedLoginAt.ShouldBeNull();
    }

    [Theory]
    [InlineData("  User@Example.AZ ", "user@example.az")]
    [InlineData("ƏLİ@test.az", "əli@test.az")]
    [InlineData("ALI@test.az", "ali@test.az")]
    public void Normalize_Email_TrimsAndLowerCases(string input, string expected) =>
        EmailAddress.Normalize(input).ShouldBe(expected);

    // Hər cəhd bir dəqiqə sonra; son cəhdin nəticəsi qaytarılır
    private static bool FailTimes(User user, int count, DateTimeOffset start)
    {
        var lockedOut = false;
        for (var i = 0; i < count; i++)
        {
            lockedOut = user.RecordFailedLogin(start.AddMinutes(i), TestData.Lockout);
        }

        return lockedOut;
    }
}
