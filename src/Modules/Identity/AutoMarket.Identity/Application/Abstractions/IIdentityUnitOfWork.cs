using AutoMarket.BuildingBlocks.Application;

namespace AutoMarket.Identity.Application.Abstractions;

// Bir command = bir SaveChanges = bir transaksiya; outbox və audit sətirləri də burada yazılır (CONVENTIONS §7.4)
internal interface IIdentityUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
