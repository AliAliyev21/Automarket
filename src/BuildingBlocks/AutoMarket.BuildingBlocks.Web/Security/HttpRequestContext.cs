using AutoMarket.BuildingBlocks.Application;
using Microsoft.AspNetCore.Http;

namespace AutoMarket.BuildingBlocks.Web.Security;

// SEC-LOG-04: audit üçün IP (ForwardedHeaders-dən sonra, SEC-RATE-12) və user agent
internal sealed class HttpRequestContext(IHttpContextAccessor httpContextAccessor) : IRequestContext
{
    private const int MaxUserAgentLength = 512;

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
            if (string.IsNullOrEmpty(userAgent))
            {
                return null;
            }

            return userAgent.Length > MaxUserAgentLength ? userAgent[..MaxUserAgentLength] : userAgent;
        }
    }
}
