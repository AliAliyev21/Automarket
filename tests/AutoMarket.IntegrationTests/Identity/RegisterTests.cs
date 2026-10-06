using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests.Identity;

// FR-AUTH-01, SEC-AUTH-08, SEC-INP-03
public sealed class RegisterTests(ContainersFixture containers)
{
    private static readonly string[] AdminRoles = ["Admin"];

    [Fact]
    public async Task Register_ExistingEmail_SameResponseAndWarningEmail()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var email = AuthApi.NewEmail();

        using var first = await AuthApi.RegisterAsync(client, email);
        using var second = await AuthApi.RegisterAsync(client, email.ToUpperInvariant());

        // FR-AUTH-01 AC4: status və body eynidir
        second.StatusCode.ShouldBe(first.StatusCode);
        second.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldBe(await first.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var emails = await FakeEmailTransport.WaitForAsync(email, 2, TestContext.Current.CancellationToken);
        emails.ShouldContain(message => message.Subject.Contains("Registration attempt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Register_SystemFieldsInBody_Ignored()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var email = AuthApi.NewEmail();

        // SEC-INP-03: rol, id, status və təsdiq əlaməti modeldə yoxdur
        using var register = await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative), new
        {
            email,
            password = AuthApi.Password,
            name = "Mass Assignment",
            termsAccepted = true,
            id = "00000000-0000-0000-0000-000000000001",
            role = "Admin",
            roles = AdminRoles,
            status = "Active",
            emailConfirmed = true,
        }, TestContext.Current.CancellationToken);
        register.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        using (var unconfirmedLogin = await AuthApi.LoginAsync(client, email))
        {
            (await AuthApi.ReadProblemAsync(unconfirmedLogin)).Code.ShouldBe("EMAIL_NOT_CONFIRMED");
        }

        await AuthApi.ConfirmEmailAsync(client, await AuthApi.WaitForConfirmationTokenAsync(email));
        using var login = await AuthApi.LoginAsync(client, email);
        using var me = await AuthApi.GetMeAsync(client, await AuthApi.ReadAccessTokenAsync(login));
        using var body = JsonDocument.Parse(await me.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        body.RootElement.GetProperty("roles").EnumerateArray().Select(role => role.GetString()).ShouldBe(["User"]);
        body.RootElement.GetProperty("id").GetString().ShouldNotBe("00000000-0000-0000-0000-000000000001");
    }

    [Fact]
    public async Task Register_CommonPassword_ValidationFailedWithFieldError()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();

        using var response = await AuthApi.RegisterAsync(client, AuthApi.NewEmail(), "1234567890");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await AuthApi.ReadProblemAsync(response);
        problem.Code.ShouldBe("VALIDATION_FAILED");
        problem.Errors!.Value.GetProperty("password")[0].GetProperty("code").GetString().ShouldBe("PASSWORD_TOO_COMMON");
    }

    [Fact]
    public async Task Register_TermsNotAccepted_ValidationFailed()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();

        using var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative), new
        {
            email = AuthApi.NewEmail(),
            password = AuthApi.Password,
            name = "Leyla",
            termsAccepted = false,
        }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AuthApi.ReadProblemAsync(response)).Errors!.Value.GetProperty("termsAccepted")[0].GetProperty("code").GetString().ShouldBe("MUST_BE_TRUE");
    }
}
