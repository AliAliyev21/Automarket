using AutoMarket.BuildingBlocks.Domain;

namespace AutoMarket.Identity.Domain.Tokens;

// SEC-AUTH-05: 256 bit opaque token, serverdə SHA-256 hash-i. Bir login = bir token ailəsi (family); hər refresh
// eyni ailədə yeni token yaradır (rotation). ExpiresAt sliding (14 gün), AbsoluteExpiresAt ailə üçün sabitdir (60 gün)
internal sealed class RefreshToken : AggregateRoot
{
    private RefreshToken()
    {
    }

    public Guid UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public Guid? ParentId { get; private set; }

    public byte[] TokenHash { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset AbsoluteExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public RefreshTokenRevocationReason? RevokedReason { get; private set; }

    public Guid? ReplacedById { get; private set; }

    public string? CreatedIp { get; private set; }

    public string? UserAgent { get; private set; }

    public static RefreshToken Issue(
        Guid id,
        Guid userId,
        Guid familyId,
        Guid? parentId,
        byte[] tokenHash,
        DateTimeOffset now,
        TimeSpan slidingLifetime,
        DateTimeOffset absoluteExpiresAt,
        string? createdIp,
        string? userAgent)
    {
        var slidingExpiresAt = now + slidingLifetime;

        return new RefreshToken
        {
            Id = id,
            UserId = userId,
            FamilyId = familyId,
            ParentId = parentId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = slidingExpiresAt < absoluteExpiresAt ? slidingExpiresAt : absoluteExpiresAt,
            AbsoluteExpiresAt = absoluteExpiresAt,
            CreatedIp = createdIp,
            UserAgent = userAgent,
        };
    }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt && now < AbsoluteExpiresAt;

    // FR-AUTH-04 AC2: rotation ilə əvəz olunmuş tokenin təkrar təqdimi reuse sayılır
    public bool WasRotated => RevokedReason == RefreshTokenRevocationReason.Rotated;

    public void Revoke(DateTimeOffset now, RefreshTokenRevocationReason reason)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevokedReason = reason;
    }
}
