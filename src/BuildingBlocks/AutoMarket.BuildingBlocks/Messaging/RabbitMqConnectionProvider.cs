using RabbitMQ.Client;

namespace AutoMarket.BuildingBlocks.Messaging;

// Proses üçün bir paylaşılan bağlantı (ADR-0005). Bağlantı qırılanda növbəti çağırış yenisini açır;
// publisher və consumer dövrləri bərpanı özləri idarə edir, ona görə AutomaticRecovery söndürülüb
public sealed class RabbitMqConnectionProvider : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnectionProvider(ConnectionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        factory.AutomaticRecoveryEnabled = false;
        factory.TopologyRecoveryEnabled = false;
        _factory = factory;
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } current)
        {
            return current;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true } existing)
            {
                return existing;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
