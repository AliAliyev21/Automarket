namespace AutoMarket.BuildingBlocks.Application;

// Açıq transaksiya yalnız bir neçə yazma addımı (məs. atomik UPDATE + SaveChanges) zəruri olduqda (CONVENTIONS §7.4).
// Commit olunmadan dispose edilən transaksiya geri qaytarılır
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    public Task CommitAsync(CancellationToken cancellationToken);
}
