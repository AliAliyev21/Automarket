using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMarket.IntegrationTests.Infrastructure;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AutoMarket.IntegrationTests.Identity;

// FR-ACC-01 AC3, SEC-AUTH-04, SEC-AUTHZ-04
public sealed class MeTests(ContainersFixture containers)
{
    private static readonly string[] AdminRoles = ["Admin"];

    [Fact]
    public async Task GetMe_Authenticated_ReturnsProfileWithoutInternalFields()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        using var response = await AuthApi.GetMeAsync(client, session.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var properties = body.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray();
        properties.ShouldBe(["email", "id", "name", "phone", "registeredAt", "roles"]);
        body.RootElement.GetProperty("email").GetString().ShouldBe(session.Email);
        body.RootElement.GetProperty("name").GetString().ShouldBe("Test İstifadəçi");
    }

    [Fact]
    public async Task GetMe_Anonymous_Returns401Unauthorized()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();

        using var response = await AuthApi.GetMeAsync(client, accessToken: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.ReadProblemAsync(response)).Code.ShouldBe("UNAUTHORIZED");
    }

    [Fact]
    public async Task GetMe_TokenSignedWithUnknownKey_Returns401()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);
        var original = new JsonWebToken(session.AccessToken);

        var forged = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = original.Issuer,
            Audience = original.Audiences.Single(),
            Expires = original.ValidTo,
            Claims = new Dictionary<string, object> { ["sub"] = original.Subject, ["role"] = AdminRoles },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) { KeyId = original.Kid },
                SecurityAlgorithms.HmacSha256),
        });

        using var response = await AuthApi.GetMeAsync(client, forged);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_UnsignedAlgNoneToken_Returns401()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        // SEC-AUTH-04: "none" alqoritmi qəbul edilmir
        var payload = session.AccessToken.Split('.')[1];
        var header = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));

        using var response = await AuthApi.GetMeAsync(client, $"{header}.{payload}.");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
