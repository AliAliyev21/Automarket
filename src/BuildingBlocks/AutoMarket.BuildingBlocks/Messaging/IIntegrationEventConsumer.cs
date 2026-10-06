namespace AutoMarket.BuildingBlocks.Messaging;

// Consumer-in biznes məntiqi (<Event>Consumer, CONVENTIONS §2.4). Inbox və transaksiya consumer host-dadır,
// consumer onları təkrar açmır və SaveChanges çağırmır (CONVENTIONS §7.4)
public interface IIntegrationEventConsumer<in TEvent>
    where TEvent : IIntegrationEvent
{
    public Task ConsumeAsync(TEvent integrationEvent, MessageContext context, CancellationToken cancellationToken);
}

public sealed record MessageContext(Guid MessageId, DateTimeOffset OccurredAt, string? CorrelationId);
