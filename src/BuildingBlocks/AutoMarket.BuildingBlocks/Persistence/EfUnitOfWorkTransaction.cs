using AutoMarket.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore.Storage;

namespace AutoMarket.BuildingBlocks.Persistence;

public sealed class EfUnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
