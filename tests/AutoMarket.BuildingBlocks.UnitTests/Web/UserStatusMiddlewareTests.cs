using System.Security.Claims;
using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMarket.BuildingBlocks.UnitTests.Web;

// SEC-AUTH-06, FR-ADM-01 AC4: bloklanmış istifadəçinin access tokeni ömrü bitənə qədər də rədd olunur
public sealed class UserStatusMiddlewareTests
{
    private static readonly Guid UserId = Guid.CreateVersion7(new DateTimeOffset(2026, 10, 6, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Invoke_Anonymous_PassesWithoutStatusLookup()
    {
        var reader = new FakeStatusReader(UserAccessStatus.Blocked);
        var (context, nextCalled) = await InvokeAsync(reader, authenticated: false);

        nextCalled.ShouldBeTrue();
        reader.Calls.ShouldBe(0);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Invoke_ActiveUser_Passes()
    {
        var (_, nextCalled) = await InvokeAsync(new FakeStatusReader(UserAccessStatus.Active), authenticated: true);

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Invoke_BlockedUser_401AccountBlocked()
    {
        var (context, nextCalled) = await InvokeAsync(new FakeStatusReader(UserAccessStatus.Blocked), authenticated: true);

        nextCalled.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        (await ReadBodyAsync(context)).ShouldContain(ErrorCodes.AccountBlocked);
    }

    [Fact]
    public async Task Invoke_InactiveUser_401Unauthorized()
    {
        var (context, nextCalled) = await InvokeAsync(new FakeStatusReader(UserAccessStatus.Inactive), authenticated: true);

        nextCalled.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        (await ReadBodyAsync(context)).ShouldContain(ErrorCodes.Unauthorized);
    }

    [Fact]
    public void ValidationFailedFor_FieldErrors_WrittenToProblemDetails()
    {
        var error = CommonErrors.ValidationFailedFor("newPassword", [new FieldError("PASSWORD_TOO_COMMON", "Too common.")]);

        var problem = error.ToProblemDetails();

        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Extensions[ProblemDetailsSetup.ErrorsKey].ShouldBeAssignableTo<IReadOnlyDictionary<string, IReadOnlyList<FieldError>>>()
            .ShouldNotBeNull()["newPassword"].ShouldHaveSingleItem().Code.ShouldBe("PASSWORD_TOO_COMMON");
    }

    private static async Task<(HttpContext Context, bool NextCalled)> InvokeAsync(IUserStatusReader reader, bool authenticated)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .AddSingleton(reader)
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();
        if (authenticated)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(HttpCurrentUser.SubjectClaim, UserId.ToString())], "Bearer"));
        }

        var nextCalled = false;
        var middleware = new UserStatusMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);
        return (context, nextCalled);
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    private sealed class FakeStatusReader(UserAccessStatus status) : IUserStatusReader
    {
        public int Calls { get; private set; }

        public Task<UserAccessStatus> GetStatusAsync(Guid userId, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(status);
        }
    }
}
