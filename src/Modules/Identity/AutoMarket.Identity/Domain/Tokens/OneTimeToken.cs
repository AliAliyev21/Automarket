using AutoMarket.BuildingBlocks.Domain;

namespace AutoMarket.Identity.Domain.Tokens;

// SEC-AUTH-07: birdəfəlik token. Serverdə yalnız SHA-256 hash-i saxlanılır, vaxtı məhduddur, bir dəfə istifadə olunur
internal sealed class OneTimeToken : AggregateRoot
{
    private OneTimeToken()
    {
    }

    public Guid UserId { get; private set; }

    public OneTimeTokenPurpose Purpose { get; private set; }

    public byte[] TokenHash { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public static OneTimeToken Issue(
        Guid id,
        Guid userId,
        OneTimeTokenPurpose purpose,
        byte[] tokenHash,
        DateTimeOffset now,
        DateTimeOffset expiresAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            Purpose = purpose,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = expiresAt,
        };

    public bool IsUsable(DateTimeOffset now) => UsedAt is null && RevokedAt is null && now < ExpiresAt;

    public void Use(DateTimeOffset now)
    {
        if (!IsUsable(now))
        {
            throw new InvalidOperationException("The token is not usable.");
        }

        UsedAt = now;
    }

    // FR-AUTH-02 AC3: yeni token köhnəsini etibarsız edir
    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
