namespace AutoMarket.IntegrationTests.Authorization;

// REQUIREMENTS 2.2 icazə matrisi test datası kimi (ARCHITECTURE §10.3). Yeni endpoint buraya əlavə olunmadan
// EndpointCoverageTests keçmir
internal static class AuthorizationMatrix
{
    public static readonly EndpointAccess[] Endpoints =
    [
        // Sətir 1: qeydiyyat, email təsdiqi — Guest
        new(1, "POST", "/api/v1/auth/register", Access.Anonymous),
        new(1, "POST", "/api/v1/auth/confirm-email", Access.Anonymous),
        new(1, "POST", "/api/v1/auth/resend-confirmation", Access.Anonymous),

        // Sətir 2: login (Guest), refresh (kimlik refresh cookie-dən gəlir, access token tələb olunmur)
        new(2, "POST", "/api/v1/auth/login", Access.Anonymous),
        new(2, "POST", "/api/v1/auth/refresh", Access.Anonymous),

        // Sətir 3: öz profilini görmək — User, Moderator, Admin (Öz)
        new(3, "GET", "/api/v1/me", Access.User),
    ];

    // Biznes endpoint-i olmayan infrastruktur route-ları (NFR-HC, NFR-DOC)
    public static readonly (string Method, string Route)[] Infrastructure =
    [
        ("GET", "/ping"),
        ("*", "/health/live"),
        ("*", "/health/ready"),
    ];
}

internal enum Access
{
    Anonymous,
    User,
}

internal sealed record EndpointAccess(int MatrixRow, string Method, string Route, Access Access);
