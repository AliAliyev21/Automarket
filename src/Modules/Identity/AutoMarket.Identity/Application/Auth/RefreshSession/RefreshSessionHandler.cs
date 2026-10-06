using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;

namespace AutoMarket.Identity.Application.Auth.RefreshSession;

// FR-AUTH-04: rotation və reuse detection (ARCHITECTURE §6.1). Köhnə token atomik UPDATE ilə ləğv olunur və yeni token
// eyni transaksiyada yazılır. Rotation ilə əvəz olunmuş token yenidən gələrsə (o cümlədən paralel ikinci sorğu, §13 A6)
// istifadəçinin bütün aktiv refresh tokenləri ləğv olunur (FR-AUTH-04 AC2: bütün sessiyalardan çıxış)
internal sealed class RefreshSessionHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    SessionService sessions,
    IIdentityUnitOfWork unitOfWork,
    IRateLimitService rateLimits,
    IAuditLog audit,
    IIdGenerator ids,
    TimeProvider time) : ICommandHandler<RefreshSessionCommand, SessionTokens>
{
    public async Task<Result<SessionTokens>> HandleAsync(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var token = await refreshTokens.FindByHashAsync(TokenHasher.Hash(command.RefreshToken), cancellationToken);
        if (token is null || !TokenHasher.Matches(command.RefreshToken, token.TokenHash))
        {
            return CommonErrors.Unauthorized;
        }

        // SEC-RATE-04: limit tokenin sahibi üzrədir
        var decision = await rateLimits.TryAcquireAsync(RateLimitRules.Refresh, RateLimitKeys.ForUser(token.UserId), cancellationToken);
        if (!decision.IsAllowed)
        {
            return CommonErrors.RateLimitedFor(decision.RetryAfter);
        }

        if (token.WasRotated)
        {
            return await RevokeAllSessionsAsync(token, now, transaction, cancellationToken);
        }

        if (!token.IsActive(now))
        {
            return CommonErrors.Unauthorized;
        }

        // FR-AUTH-04 AC3: təsdiqlənməmiş və bloklanmış istifadəçinin tokeni qəbul edilmir
        var user = await users.GetByIdAsync(token.UserId, cancellationToken);
        if (user is not { Status: UserStatus.Active })
        {
            return CommonErrors.Unauthorized;
        }

        var newTokenId = ids.NewId();
        if (!await refreshTokens.TryRevokeForRotationAsync(token.Id, newTokenId, now, cancellationToken))
        {
            return await RevokeAllSessionsAsync(token, now, transaction, cancellationToken);
        }

        var tokens = await sessions.RotateAsync(token, newTokenId, now, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return tokens;
    }

    private async Task<Result<SessionTokens>> RevokeAllSessionsAsync(
        RefreshToken token,
        DateTimeOffset now,
        IUnitOfWorkTransaction transaction,
        CancellationToken cancellationToken)
    {
        await refreshTokens.RevokeAllActiveAsync(token.UserId, RefreshTokenRevocationReason.ReuseDetected, now, cancellationToken);

        audit.Record(new AuditEntry(
            AuthAuditEvents.RefreshTokenReused,
            AuthAuditEvents.ResultFailure,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: token.UserId.ToString(),
            Details: new Dictionary<string, string>(StringComparer.Ordinal) { ["familyId"] = token.FamilyId.ToString() }));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AuthErrors.RefreshTokenReused;
    }
}
