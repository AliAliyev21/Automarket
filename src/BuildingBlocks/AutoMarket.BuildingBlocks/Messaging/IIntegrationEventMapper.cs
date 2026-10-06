using AutoMarket.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace AutoMarket.BuildingBlocks.Messaging;

// Modulun domen hadisəsini integration event-ə çevirir; outbox interceptor-u çağırır (CONVENTIONS §5.6)
public interface IIntegrationEventMapper<TContext>
    where TContext : DbContext
{
    public IEnumerable<IIntegrationEvent> Map(IDomainEvent domainEvent);
}
