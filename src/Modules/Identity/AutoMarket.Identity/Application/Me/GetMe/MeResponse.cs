namespace AutoMarket.Identity.Application.Me.GetMe;

// FR-ACC-01 AC3, SEC-AUTHZ-04: şifrə hash-i, token, lockout sayğacı kimi daxili sahələr yoxdur. Email və telefon yalnız sahibinə
internal sealed record MeResponse(
    Guid Id,
    string Email,
    string Name,
    string? Phone,
    IReadOnlyList<string> Roles,
    DateTimeOffset RegisteredAt);
