namespace AutoMarket.BuildingBlocks.Messaging;

// Routing key: <modul>.<aggregate>.<hadisə>.v<versiya> (ARCHITECTURE §5.3). Ad və versiya event tipində bir dəfə yazılır
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IntegrationEventAttribute(string name, int version) : Attribute
{
    public string Name { get; } = name;

    public int Version { get; } = version;

    public string RoutingKey => $"{Name}.v{Version}";
}
