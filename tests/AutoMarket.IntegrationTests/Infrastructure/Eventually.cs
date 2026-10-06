namespace AutoMarket.IntegrationTests.Infrastructure;

// Asinxron axınlar (outbox → RabbitMQ → consumer) üçün gözləmə helper-i: şərt ödənənə və ya timeout bitənə qədər
internal static class Eventually
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    public static async Task<T> WaitAsync<T>(Func<Task<T>> probe, Func<T, bool> condition, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        while (true)
        {
            var value = await probe();
            if (condition(value))
            {
                return value;
            }

            try
            {
                await Task.Delay(PollInterval, timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Condition was not met within {Timeout.TotalSeconds} seconds.");
            }
        }
    }
}
