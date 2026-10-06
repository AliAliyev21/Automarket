using System.Net;
using System.Net.Http.Json;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests;

public sealed class PingTests(ContainersFixture containers)
{
    [Fact]
    public async Task Ping_Anonymous_ReturnsPong()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/ping", UriKind.Relative), cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<string>(cancellationToken)).ShouldBe("pong");
    }
}
