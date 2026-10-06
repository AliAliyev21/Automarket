namespace AutoMarket.IntegrationTests.Infrastructure;

// Formatı düzgün, amma heç nə dinləməyən ünvanlar: asılılığın düşdüyü vəziyyəti simulyasiya edir
public static class UnreachableEndpoints
{
    public const string Postgres = "Host=127.0.0.1;Port=1;Database=automarket;Username=automarket;Password=unreachable;Timeout=2";

    public const string Redis = "127.0.0.1:1,password=unreachable,connectTimeout=1000";

    public const string RabbitMq = "amqp://automarket:unreachable@127.0.0.1:1/";
}
