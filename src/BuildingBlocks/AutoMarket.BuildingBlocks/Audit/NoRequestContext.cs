using AutoMarket.BuildingBlocks.Application;

namespace AutoMarket.BuildingBlocks.Audit;

// HTTP xaricində (consumer, job) IP və user agent yoxdur; BuildingBlocks.Web bunu HTTP implementasiyası ilə əvəz edir
internal sealed class NoRequestContext : IRequestContext
{
    public string? IpAddress => null;

    public string? UserAgent => null;
}
