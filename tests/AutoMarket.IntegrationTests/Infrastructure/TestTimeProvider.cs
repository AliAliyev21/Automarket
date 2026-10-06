namespace AutoMarket.IntegrationTests.Infrastructure;

// NFR-TEST-08: saat testdə idarə olunur. FakeTimeProvider background servislərin gözləmələrini dondurardı (outbox, consumer),
// ona görə real saat + sürüşmə istifadə olunur: Advance yalnız "indi"-ni dəyişir, timer-lər real vaxtla işləyir
public sealed class TestTimeProvider : TimeProvider
{
    private long _offsetTicks;

    public override DateTimeOffset GetUtcNow() => System.GetUtcNow() + TimeSpan.FromTicks(Interlocked.Read(ref _offsetTicks));

    public void Advance(TimeSpan delta) => Interlocked.Add(ref _offsetTicks, delta.Ticks);
}
