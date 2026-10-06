using AutoMarket.Identity.Application.Me.GetMe;

namespace AutoMarket.Identity.Application.Abstractions;

internal interface IUserQueries
{
    public Task<MeResponse?> GetMeAsync(Guid userId, CancellationToken cancellationToken);
}
