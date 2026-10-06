using System.Net;
using System.Text.Json;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests.Identity;

// FR-ADM-01, FR-ADM-02, R-04, R-05, SEC-AUTH-06, SEC-AUTHZ-01
public sealed class AdminUsersTests(ContainersFixture containers)
{
    [Fact]
    public async Task Block_ActiveSession_AccessTokenRejectedImmediately()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var admin = await CreateAdminAsync(client);
        var target = await AuthApi.RegisterConfirmAndLoginAsync(client);
        var targetId = await AuthApi.GetUserIdAsync(client, target.AccessToken);

        using var block = await BlockAsync(client, admin, targetId);

        block.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // FR-ADM-01 AC4: eyni (hələ vaxtı keçməmiş) access token rədd olunur
        using var me = await AuthApi.GetMeAsync(client, target.AccessToken);
        me.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.ReadProblemAsync(me)).Code.ShouldBe("ACCOUNT_BLOCKED");

        // AC2: refresh tokenlər ləğv olunub
        (await AuthApi.RefreshAsync(client, target.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Block_Login_AccountBlockedOnlyWithCorrectPassword()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var admin = await CreateAdminAsync(client);
        var target = await AuthApi.RegisterConfirmAndLoginAsync(client);
        var targetId = await AuthApi.GetUserIdAsync(client, target.AccessToken);
        (await BlockAsync(client, admin, targetId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var correct = await AuthApi.LoginAsync(client, target.Email);
        using var wrong = await AuthApi.LoginAsync(client, target.Email, "Wrong-Password-123");

        correct.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.ReadProblemAsync(correct)).Code.ShouldBe("ACCOUNT_BLOCKED");
        (await AuthApi.ReadProblemAsync(wrong)).Code.ShouldBe("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Unblock_BlockedUser_CanLoginAgain()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var admin = await CreateAdminAsync(client);
        var target = await AuthApi.RegisterConfirmAndLoginAsync(client);
        var targetId = await AuthApi.GetUserIdAsync(client, target.AccessToken);
        (await BlockAsync(client, admin, targetId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var unblock = await AuthApi.SendAsync(client, HttpMethod.Post, $"/api/v1/admin/users/{targetId}/unblock", admin.AccessToken);

        unblock.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await AuthApi.LoginAsync(client, target.Email)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Block_PublishesEventsAndAudits()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var admin = await CreateAdminAsync(client);
        var target = await AuthApi.RegisterConfirmAndLoginAsync(client);
        var targetId = await AuthApi.GetUserIdAsync(client, target.AccessToken);

        (await BlockAsync(client, admin, targetId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await AuthApi.SendAsync(client, HttpMethod.Post, $"/api/v1/admin/users/{targetId}/unblock", admin.AccessToken))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await IdentityDatabase.CountOutboxAsync(containers.PostgresConnectionString, "identity.user.blocked.v1", targetId)).ShouldBe(1);
        (await IdentityDatabase.CountOutboxAsync(containers.PostgresConnectionString, "identity.user.unblocked.v1", targetId)).ShouldBe(1);
        var audit = await Eventually.WaitAsync(
            () => IdentityDatabase.ReadAuditEventTypesAsync(containers.PostgresConnectionString, targetId),
            types => types.Contains("admin.user_blocked") && types.Contains("admin.user_unblocked"),
            TestContext.Current.CancellationToken);
        audit.ShouldContain("admin.user_blocked");
    }

    [Fact]
    public async Task Block_Self_Forbidden()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var admin = await CreateAdminAsync(client);
        var adminId = await AuthApi.GetUserIdAsync(client, admin.AccessToken);

        using var block = await BlockAsync(client, admin, adminId);

        block.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await AuthApi.GetMeAsync(client, admin.AccessToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Block_UnknownUser_UserNotFound()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var admin = await CreateAdminAsync(client);

        using var block = await BlockAsync(client, admin, Guid.CreateVersion7());

        block.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await AuthApi.ReadProblemAsync(block)).Code.ShouldBe("USER_NOT_FOUND");
    }

    [Fact]
    public async Task Block_MissingReason_ValidationFailed()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var admin = await CreateAdminAsync(client);

        using var block = await AuthApi.SendAsync(
            client, HttpMethod.Post, $"/api/v1/admin/users/{Guid.CreateVersion7()}/block", admin.AccessToken, new { reason = "" });

        block.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AuthApi.ReadProblemAsync(block)).Code.ShouldBe("VALIDATION_FAILED");
    }

    [Fact]
    public async Task Block_ByModerator_Forbidden()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var moderator = await CreateWithRoleAsync(client, IdentityDatabase.ModeratorRoleId);
        var target = await AuthApi.RegisterConfirmAndLoginAsync(client);
        var targetId = await AuthApi.GetUserIdAsync(client, target.AccessToken);

        using var block = await BlockAsync(client, moderator, targetId);

        block.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await AuthApi.GetMeAsync(client, target.AccessToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GrantRole_Moderator_AppearsInNewAccessToken()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var admin = await CreateAdminAsync(client);
        var target = await AuthApi.RegisterConfirmAndLoginAsync(client);
        var targetId = await AuthApi.GetUserIdAsync(client, target.AccessToken);

        using var grant = await AuthApi.SendAsync(
            client, HttpMethod.Post, $"/api/v1/admin/users/{targetId}/roles", admin.AccessToken, new { role = "Moderator" });

        grant.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var refresh = await AuthApi.RefreshAsync(client, target.RefreshToken);
        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
        ReadRoles(await AuthApi.ReadAccessTokenAsync(refresh)).ShouldContain("Moderator");
        (await IdentityDatabase.CountOutboxAsync(containers.PostgresConnectionString, "identity.user.roles-changed.v1", targetId)).ShouldBe(1);
    }

    [Fact]
    public async Task GrantRole_UnknownRole_ValidationFailed()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var admin = await CreateAdminAsync(client);
        var adminId = await AuthApi.GetUserIdAsync(client, admin.AccessToken);

        using var grant = await AuthApi.SendAsync(
            client, HttpMethod.Post, $"/api/v1/admin/users/{adminId}/roles", admin.AccessToken, new { role = "SuperAdmin" });

        grant.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RevokeRole_Downgrade_RevokesSessions()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var admin = await CreateAdminAsync(client);
        var moderator = await CreateWithRoleAsync(client, IdentityDatabase.ModeratorRoleId);
        var moderatorId = await AuthApi.GetUserIdAsync(client, moderator.AccessToken);

        using var revoke = await AuthApi.SendAsync(
            client, HttpMethod.Delete, $"/api/v1/admin/users/{moderatorId}/roles/Moderator", admin.AccessToken);

        revoke.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await AuthApi.RefreshAsync(client, moderator.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var login = await AuthApi.LoginAsync(client, moderator.Email);
        ReadRoles(await AuthApi.ReadAccessTokenAsync(login)).ShouldNotContain("Moderator");
    }

    [Fact]
    public async Task RevokeRole_OwnAdminRole_Forbidden()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var admin = await CreateAdminAsync(client);
        var adminId = await AuthApi.GetUserIdAsync(client, admin.AccessToken);

        using var revoke = await AuthApi.SendAsync(client, HttpMethod.Delete, $"/api/v1/admin/users/{adminId}/roles/Admin", admin.AccessToken);

        revoke.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeRole_UnknownRole_ValidationFailed()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var admin = await CreateAdminAsync(client);

        using var revoke = await AuthApi.SendAsync(
            client, HttpMethod.Delete, $"/api/v1/admin/users/{Guid.CreateVersion7()}/roles/User", admin.AccessToken);

        revoke.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AuthApi.ReadProblemAsync(revoke)).Code.ShouldBe("VALIDATION_FAILED");
    }

    private static Task<HttpResponseMessage> BlockAsync(HttpClient client, AuthApi.Session admin, Guid userId) =>
        AuthApi.SendAsync(client, HttpMethod.Post, $"/api/v1/admin/users/{userId}/block", admin.AccessToken, new { reason = "Fraudulent listings" });

    private Task<AuthApi.Session> CreateAdminAsync(HttpClient client) => CreateWithRoleAsync(client, IdentityDatabase.AdminRoleId);

    // Rol DB-də verilir, sonra yenidən login olunur ki, access token-də rol olsun
    private async Task<AuthApi.Session> CreateWithRoleAsync(HttpClient client, Guid roleId)
    {
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);
        await IdentityDatabase.GrantRoleAsync(containers.PostgresConnectionString, await AuthApi.GetUserIdAsync(client, session.AccessToken), roleId);

        using var login = await AuthApi.LoginAsync(client, session.Email);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        return new AuthApi.Session(session.Email, await AuthApi.ReadAccessTokenAsync(login), AuthApi.GetRefreshToken(login)!);
    }

    // Access token-in role claim-i (SEC-AUTH-04): rol dəyişikliyi yeni tokendə görünür
    private static IReadOnlyList<string> ReadRoles(string accessToken)
    {
        var payload = accessToken.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        using var body = JsonDocument.Parse(Convert.FromBase64String(payload));
        var role = body.RootElement.GetProperty("role");

        return role.ValueKind == JsonValueKind.Array
            ? [.. role.EnumerateArray().Select(item => item.GetString()!)]
            : [role.GetString()!];
    }

    // ARCHITECTURE §11: aktiv Admin varsa ilk Admin əmri rədd olunur (testlər paylaşılan bazada işlədiyi üçün yalnız bu yol yoxlanılır,
    // yaratma yolu unit testlərdədir)
    [Fact]
    public async Task BootstrapAdmin_ActiveAdminExists_Refused()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        await CreateAdminAsync(client);
        var email = AuthApi.NewEmail();

        var (succeeded, message) = await AutoMarket.Identity.IdentityModule.BootstrapAdminAsync(
            factory.Services, email, AuthApi.Password, "Administrator", TestContext.Current.CancellationToken);

        succeeded.ShouldBeFalse();
        message.ShouldStartWith("FORBIDDEN");
        (await AuthApi.LoginAsync(client, email)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
