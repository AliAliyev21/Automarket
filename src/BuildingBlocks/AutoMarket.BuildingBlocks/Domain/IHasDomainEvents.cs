namespace AutoMarket.BuildingBlocks.Domain;

// AggregateRoot-dan törəyə bilməyən aggregate-lər üçün (məs. IdentityUser-dən törəyən User, ADR-0003)
public interface IHasDomainEvents
{
    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    public void ClearDomainEvents();
}
