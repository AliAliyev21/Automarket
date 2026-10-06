namespace AutoMarket.BuildingBlocks.Messaging;

// Bütün interval və limitlər konfiqurasiyadan oxunur (ARCHITECTURE §5.2, §5.3)
public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public OutboxSettings Outbox { get; set; } = new();

    public ConsumerSettings Consumer { get; set; } = new();

    // Retry queue-larının TTL-i; say = maksimum cəhd sayı, sonra DLQ
    public IList<TimeSpan> RetryDelays { get; } = [];

    // Broker əlçatmaz olanda yenidən qoşulmaya qədər gözləmə
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

    public sealed class OutboxSettings
    {
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

        public TimeSpan IdleDelay { get; set; } = TimeSpan.FromSeconds(5);

        public int BatchSize { get; set; } = 100;
    }

    public sealed class ConsumerSettings
    {
        public ushort Prefetch { get; set; } = 16;
    }
}
