using AutoMarket.BuildingBlocks.Domain;
using AutoMarket.Identity.Domain.Users.Events;
using Microsoft.AspNetCore.Identity;

namespace AutoMarket.Identity.Domain.Users;

// ADR-0003: Identity Core-un EF store-ları üçün IdentityUser-dən törəyir (ARCHITECTURE §10.4-də Domain istisnası).
// Email, NormalizedEmail, PasswordHash, SecurityStamp, AccessFailedCount, LockoutEnd Identity-nin sahələridir.
// Identity-nin daxili lockout-u söndürülüb: eskalasiya olunan lockout bu sinifdədir (SEC-AUTH-03)
internal sealed class User : IdentityUser<Guid>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private User()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public UserStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public int LockoutLevel { get; private set; }

    public DateTimeOffset? LastFailedLoginAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    // FR-AUTH-01 AC5/AC6: hesab təsdiqlənməmiş statusda yaranır; rol (yalnız User) Infrastructure-da təyin olunur
    public static User Register(Guid id, string normalizedEmail, string name, string? phone, DateTimeOffset now)
    {
        var user = new User
        {
            Id = id,
            Email = normalizedEmail,
            UserName = normalizedEmail,
            Name = name,
            PhoneNumber = phone,
            Status = UserStatus.Unconfirmed,
            CreatedAt = now,
            EmailConfirmed = false,
            LockoutEnabled = false,
        };

        user._domainEvents.Add(new UserRegisteredDomainEvent(id, now));
        return user;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    // Xam token yalnız yaddaşdakı domen hadisəsindədir; outbox-a Data Protection ilə şifrələnmiş halda yazılır (ARCHITECTURE §5.4)
    public void RequestEmailConfirmation(string rawToken, DateTimeOffset expiresAt) =>
        _domainEvents.Add(new EmailConfirmationRequestedDomainEvent(Id, Email!, rawToken, expiresAt));

    // FR-AUTH-01 AC4: mövcud hesabın sahibinə "kimsə bu email ilə qeydiyyatdan keçməyə çalışdı" məktubu
    public void RecordRegistrationAttempt() =>
        _domainEvents.Add(new RegistrationAttemptedOnExistingAccountDomainEvent(Id, Email!));

    // FR-AUTH-02 AC2
    public void ConfirmEmail(DateTimeOffset now)
    {
        if (Status != UserStatus.Unconfirmed)
        {
            return;
        }

        Status = UserStatus.Active;
        EmailConfirmed = true;
        ConfirmedAt = now;
        _domainEvents.Add(new UserEmailConfirmedDomainEvent(Id));
    }

    public bool IsLockedOut(DateTimeOffset now) => LockoutEnd is { } end && end > now;

    // SEC-AUTH-03: 15 dəqiqə ərzində 5 ardıcıl uğursuz cəhd → kilid 15 dəq × 2^(level−1), ən çox 24 saat.
    // Son uğursuz cəhddən pəncərədən çox vaxt keçibsə sayğac yenidən başlayır. Kilid yarandısa true qaytarır
    public bool RecordFailedLogin(DateTimeOffset now, LockoutPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (LastFailedLoginAt is { } lastFailedAt && now - lastFailedAt > policy.FailureWindow)
        {
            AccessFailedCount = 0;
        }

        AccessFailedCount++;
        LastFailedLoginAt = now;

        if (AccessFailedCount < policy.MaxFailedAttempts)
        {
            return false;
        }

        LockoutLevel++;
        AccessFailedCount = 0;
        LockoutEnd = now + policy.DurationForLevel(LockoutLevel);
        _domainEvents.Add(new UserLockedOutDomainEvent(Id, Email!, LockoutEnd.Value));

        return true;
    }

    // Uğurlu login sayğacı və eskalasiya səviyyəsini sıfırlayır
    public void RecordSuccessfulLogin()
    {
        AccessFailedCount = 0;
        LockoutLevel = 0;
        LastFailedLoginAt = null;
        LockoutEnd = null;
    }
}
