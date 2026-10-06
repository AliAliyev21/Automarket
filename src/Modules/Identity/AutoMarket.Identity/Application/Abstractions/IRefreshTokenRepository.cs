using AutoMarket.Identity.Domain.Tokens;

namespace AutoMarket.Identity.Application.Abstractions;

internal interface IRefreshTokenRepository
{
    // Tracking olmadan oxunur: dəyişiklik yalnız atomik UPDATE ilə aparılır
    public Task<RefreshToken?> FindByHashAsync(byte[] tokenHash, CancellationToken cancellationToken);

    public void Add(RefreshToken token);

    // Köhnədən yeniyə sıralanmış aktiv tokenlər (hər ailədə bir aktiv token = bir sessiya)
    public Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    // ARCHITECTURE §6.1: UPDATE ... WHERE revoked_at IS NULL — paralel ikinci sorğu false alır və reuse sayılır
    public Task<bool> TryRevokeForRotationAsync(Guid tokenId, Guid replacedById, DateTimeOffset now, CancellationToken cancellationToken);

    public Task<int> RevokeAllActiveAsync(Guid userId, RefreshTokenRevocationReason reason, DateTimeOffset now, CancellationToken cancellationToken);

    // FR-AUTH-07 AC2: cari sessiya (ailə) saxlanılır, qalanları ləğv olunur
    public Task<int> RevokeAllActiveExceptFamilyAsync(
        Guid userId,
        Guid keptFamilyId,
        RefreshTokenRevocationReason reason,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    // FR-AUTH-05 AC1: bir sessiya = bir ailə
    public Task<int> RevokeFamilyAsync(Guid familyId, RefreshTokenRevocationReason reason, DateTimeOffset now, CancellationToken cancellationToken);
}
