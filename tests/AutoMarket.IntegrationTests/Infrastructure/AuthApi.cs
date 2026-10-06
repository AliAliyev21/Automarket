using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoMarket.IntegrationTests.Infrastructure;

// Auth endpoint-ləri üçün test client-i. Refresh cookie Set-Cookie-dən əl ilə oxunur və Cookie header-i ilə göndərilir
internal static partial class AuthApi
{
    public const string Password = "Gx7!kq2#z9-Strong";
    public const string RefreshCookieName = "rt";

    public static string NewEmail() => $"user-{Guid.CreateVersion7():N}@automarket.test";

    public static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string password = Password) =>
        PostJsonAsync(client, "/api/v1/auth/register", new
        {
            email,
            password,
            name = "Test İstifadəçi",
            phone = "+994501234567",
            termsAccepted = true,
        });

    public static Task<HttpResponseMessage> ConfirmEmailAsync(HttpClient client, string token) =>
        PostJsonAsync(client, "/api/v1/auth/confirm-email", new { token });

    public static Task<HttpResponseMessage> ResendConfirmationAsync(HttpClient client, string email) =>
        PostJsonAsync(client, "/api/v1/auth/resend-confirmation", new { email });

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = Password) =>
        PostJsonAsync(client, "/api/v1/auth/login", new { email, password });

    public static Task<HttpResponseMessage> RefreshAsync(HttpClient client, string? refreshToken, string? origin = ContainersFixture.AllowedOrigin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        if (refreshToken is not null)
        {
            request.Headers.Add("Cookie", $"{RefreshCookieName}={refreshToken}");
        }

        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static Task<HttpResponseMessage> LogoutAsync(HttpClient client, string? refreshToken, string? origin = ContainersFixture.AllowedOrigin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        if (refreshToken is not null)
        {
            request.Headers.Add("Cookie", $"{RefreshCookieName}={refreshToken}");
        }

        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static Task<HttpResponseMessage> LogoutAllAsync(HttpClient client, string accessToken) =>
        SendAsync(client, HttpMethod.Post, "/api/v1/auth/logout-all", accessToken);

    public static Task<HttpResponseMessage> ForgotPasswordAsync(HttpClient client, string email) =>
        PostJsonAsync(client, "/api/v1/auth/forgot-password", new { email });

    public static Task<HttpResponseMessage> ResetPasswordAsync(HttpClient client, string token, string newPassword) =>
        PostJsonAsync(client, "/api/v1/auth/reset-password", new { token, newPassword });

    public static Task<HttpResponseMessage> ChangePasswordAsync(
        HttpClient client,
        string accessToken,
        string currentPassword,
        string newPassword,
        string? refreshToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/change-password")
        {
            Content = JsonContent.Create(new { currentPassword, newPassword }),
        };
        if (refreshToken is not null)
        {
            request.Headers.Add("Cookie", $"{RefreshCookieName}={refreshToken}");
        }

        return SendAsync(client, request, accessToken);
    }

    // Bearer token ilə ixtiyari sorğu (body JSON)
    public static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, string? accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return SendAsync(client, request, accessToken);
    }

    public static async Task<Guid> GetUserIdAsync(HttpClient client, string accessToken)
    {
        using var me = await GetMeAsync(client, accessToken);
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await me.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return body.RootElement.GetProperty("id").GetGuid();
    }

    // Şifrə bərpası məktubundakı linkdən token (az+en)
    public static async Task<string> WaitForResetTokenAsync(string email, int expectedResetEmails = 1)
    {
        var messages = await Eventually.WaitAsync(
            () => Task.FromResult(FakeEmailTransport.SentTo(email).Where(message => ResetLinkPattern().IsMatch(message.TextBody)).ToList()),
            resets => resets.Count >= expectedResetEmails,
            TestContext.Current.CancellationToken);

        return Uri.UnescapeDataString(ResetLinkPattern().Match(messages[expectedResetEmails - 1].TextBody).Groups["token"].Value);
    }

    public static async Task<HttpResponseMessage> GetMeAsync(HttpClient client, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    // Təsdiq məktubundan (az+en) linkdəki token
    public static async Task<string> WaitForConfirmationTokenAsync(string email, int expectedEmails = 1)
    {
        var messages = await FakeEmailTransport.WaitForAsync(email, expectedEmails, TestContext.Current.CancellationToken);
        var match = ConfirmationLinkPattern().Match(messages[expectedEmails - 1].TextBody);
        match.Success.ShouldBeTrue("confirmation email must contain a link with a token");

        return Uri.UnescapeDataString(match.Groups["token"].Value);
    }

    // register → məktub → confirm → login
    public static async Task<Session> RegisterConfirmAndLoginAsync(HttpClient client)
    {
        var email = NewEmail();
        (await RegisterAsync(client, email)).StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await ConfirmEmailAsync(client, await WaitForConfirmationTokenAsync(email))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var login = await LoginAsync(client, email);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);

        return new Session(email, await ReadAccessTokenAsync(login), GetRefreshToken(login)!);
    }

    public static async Task<string> ReadAccessTokenAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return body.RootElement.GetProperty("accessToken").GetString()!;
    }

    public static string? GetRefreshToken(HttpResponseMessage response) =>
        GetRefreshCookieHeader(response) is { } header && header.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal)
            ? header[(RefreshCookieName.Length + 1)..header.IndexOf(';', StringComparison.Ordinal)] is { Length: > 0 } value ? value : null
            : null;

    public static string? GetRefreshCookieHeader(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value => value.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal))
            : null;

    public static async Task<Problem> ReadProblemAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var root = body.RootElement;

        return new Problem(
            root.GetProperty("code").GetString()!,
            root.GetProperty("message").GetString()!,
            root.TryGetProperty("errors", out var errors) ? errors.Clone() : null);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request, string? accessToken)
    {
        using (request)
        {
            if (accessToken is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            return await client.SendAsync(request, TestContext.Current.CancellationToken);
        }
    }

    private static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, object body) =>
        client.PostAsJsonAsync(new Uri(path, UriKind.Relative), body, TestContext.Current.CancellationToken);

    [GeneratedRegex(@"confirm-email\?token=(?<token>[^\s""&<]+)")]
    private static partial Regex ConfirmationLinkPattern();

    [GeneratedRegex(@"reset-password\?token=(?<token>[^\s""&<]+)")]
    private static partial Regex ResetLinkPattern();

    public sealed record Session(string Email, string AccessToken, string RefreshToken);

    public sealed record Problem(string Code, string Message, JsonElement? Errors);
}
