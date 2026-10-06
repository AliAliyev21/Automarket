using System.Net;
using System.Text.Json;
using AutoMarket.BuildingBlocks.Web.Correlation;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests;

// NFR-CORR, SEC-ERR-01
public sealed class CorrelationIdTests(ContainersFixture containers)
{
    [Fact]
    public async Task Request_ValidCorrelationId_EchoedInResponse()
    {
        await using var factory = containers.CreateFactory();

        using var response = await SendAsync(factory, "/ping", "client-id_123");

        response.Headers.GetValues(CorrelationIdFormat.HeaderName).ShouldHaveSingleItem().ShouldBe("client-id_123");
    }

    [Theory]
    [InlineData("contains space")]
    [InlineData("slash/not-allowed")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task Request_InvalidCorrelationId_ReplacedWithGenerated(string correlationId)
    {
        await using var factory = containers.CreateFactory();

        using var response = await SendAsync(factory, "/ping", correlationId);

        var returned = response.Headers.GetValues(CorrelationIdFormat.HeaderName).ShouldHaveSingleItem();
        returned.ShouldNotBe(correlationId);
        CorrelationIdFormat.IsValid(returned).ShouldBeTrue();
    }

    [Fact]
    public async Task UnknownRoute_Returns404ProblemDetailsWithTraceIdAndNoCode()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = containers.CreateFactory();

        using var response = await SendAsync(factory, "/does-not-exist", "trace-404");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(404);
        problem.RootElement.GetProperty("traceId").GetString().ShouldBe("trace-404");
        problem.RootElement.TryGetProperty("code", out _).ShouldBeFalse();
    }

    private static async Task<HttpResponseMessage> SendAsync(AutoMarketApiFactory factory, string path, string correlationId)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.TryAddWithoutValidation(CorrelationIdFormat.HeaderName, correlationId);

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
