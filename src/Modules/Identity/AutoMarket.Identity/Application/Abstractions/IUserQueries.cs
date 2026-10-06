using AutoMarket.BuildingBlocks.Application;
using AutoMarket.Identity.Application.Me.GetMe;

namespace AutoMarket.Identity.Application.Abstractions;

internal interface IUserQueries
{
    public Task<MeResponse?> GetMeAsync(Guid userId, CancellationToken cancellationToken);

    // SEC-AUTH-06: status middleware-i üçün (cache factory-si)
    public Task<UserAccessStatus> GetAccessStatusAsync(Guid userId, CancellationToken cancellationToken);
}
