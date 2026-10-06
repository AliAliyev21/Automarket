namespace AutoMarket.BuildingBlocks.Domain;

// Id-lər UUIDv7-dir və id generator ilə yaradılır (ARCHITECTURE §4.3, CONVENTIONS §4.1)
public abstract class Entity
{
    public Guid Id { get; protected set; }
}
