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

        // Sətir 1: şifrə bərpası — Guest
        new(1, "POST", "/api/v1/auth/forgot-password", Access.Anonymous),
        new(1, "POST", "/api/v1/auth/reset-password", Access.Anonymous),

        // Sətir 2: login (Guest), refresh (kimlik refresh cookie-dən gəlir, access token tələb olunmur)
        new(2, "POST", "/api/v1/auth/login", Access.Anonymous),
        new(2, "POST", "/api/v1/auth/refresh", Access.Anonymous),

        // Sətir 2: logout — cari sessiya refresh cookie-dən (access token vaxtı keçmiş ola bilər); bütün sessiyalar — User
        new(2, "POST", "/api/v1/auth/logout", Access.Anonymous),
        new(2, "POST", "/api/v1/auth/logout-all", Access.User),

        // Sətir 3: öz profilini görmək — User, Moderator, Admin (Öz)
        new(3, "GET", "/api/v1/me", Access.User),

        // Sətir 4: şifrəni dəyişmək — Öz
        new(4, "POST", "/api/v1/auth/change-password", Access.User),

        // Sətir 28: bloklamaq / blokdan çıxarmaq — yalnız Admin
        new(28, "POST", "/api/v1/admin/users/{id:guid}/block", Access.Admin),
        new(28, "POST", "/api/v1/admin/users/{id:guid}/unblock", Access.Admin),

        // Sətir 29: rol təyin etmək / ləğv etmək — yalnız Admin
        new(29, "POST", "/api/v1/admin/users/{id:guid}/roles", Access.Admin),
        new(29, "DELETE", "/api/v1/admin/users/{id:guid}/roles/{role}", Access.Admin),
    ];

    // Route şablonunu sorğu üçün konkret yola çevirir (mövcud olmayan id: avtorizasiya handler-dən əvvəl yoxlanılır)
    public static string ToConcreteRoute(string route) =>
        route.Replace("{id:guid}", "0199b8a0-0000-7000-8000-0000000000ff", StringComparison.Ordinal)
            .Replace("{role}", "Moderator", StringComparison.Ordinal);

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
    Admin,
}

internal sealed record EndpointAccess(int MatrixRow, string Method, string Route, Access Access);
