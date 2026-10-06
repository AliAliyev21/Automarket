using AutoMarket.BuildingBlocks.Application;
using AutoMarket.Identity.Application.Abstractions;

namespace AutoMarket.Identity.Application.Me.GetMe;

internal sealed class GetMeHandler(IUserQueries queries) : IQueryHandler<GetMeQuery, MeResponse>
{
    public async Task<Result<MeResponse>> HandleAsync(GetMeQuery query, CancellationToken cancellationToken)
    {
        // Token etibarlıdır, amma istifadəçi artıq yoxdur
        var me = await queries.GetMeAsync(query.UserId, cancellationToken);
        return me is null ? CommonErrors.Unauthorized : me;
    }
}
