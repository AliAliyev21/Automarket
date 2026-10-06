using AutoMarket.Notifications.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoMarket.IntegrationTests.Infrastructure;

// Test mühiti (NFR-ENV-04): user-secrets yüklənmir, konfiqurasiya yalnız testdən gəlir. SMTP fake ilə, saat idarə olunan ilə əvəz olunur
public sealed class AutoMarketApiFactory(IReadOnlyDictionary<string, string?> settings) : WebApplicationFactory<Program>
{
    public TestTimeProvider Time { get; } = new();

    // Refresh cookie əl ilə idarə olunur (Secure cookie-ni test client-i özü göndərmir)
    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false,
        AllowAutoRedirect = false,
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);

            services.RemoveAll<IEmailTransport>();
            services.AddSingleton<IEmailTransport, FakeEmailTransport>();
        });
    }
}
