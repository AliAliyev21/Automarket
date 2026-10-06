namespace AutoMarket.BuildingBlocks.Application;

// Guid.NewGuid qadağandır (BannedSymbols.txt); UUIDv7 zamana görə sıralanır (ARCHITECTURE §4.3)
public interface IIdGenerator
{
    public Guid NewId();
}

internal sealed class UuidV7Generator(TimeProvider time) : IIdGenerator
{
    public Guid NewId() => Guid.CreateVersion7(time.GetUtcNow());
}
