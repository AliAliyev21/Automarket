using System.Diagnostics;
using System.Text.Json;
using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Domain;
using AutoMarket.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AutoMarket.BuildingBlocks.Persistence;

// Domen hadisələri və audit qeydləri biznes datası ilə eyni SaveChanges-də outbox-a yazılır (ARCHITECTURE §5.2, CONVENTIONS §7.4)
internal sealed class OutboxInterceptor<TContext>(
    IEnumerable<IIntegrationEventMapper<TContext>> mappers,
    AuditLog auditLog,
    IIdGenerator ids,
    TimeProvider time) : SaveChangesInterceptor
    where TContext : DbContext
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is not null)
        {
            AddOutboxMessages(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is not null)
        {
            AddOutboxMessages(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddOutboxMessages(DbContext context)
    {
        var integrationEvents = new List<IIntegrationEvent>();

        var aggregates = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                foreach (var mapper in mappers)
                {
                    integrationEvents.AddRange(mapper.Map(domainEvent));
                }
            }

            aggregate.ClearDomainEvents();
        }

        integrationEvents.AddRange(auditLog.DrainPending());

        var occurredAt = time.GetUtcNow();
        foreach (var integrationEvent in integrationEvents)
        {
            var metadata = IntegrationEventMetadata.Of(integrationEvent.GetType());
            context.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = ids.NewId(),
                Type = metadata.RoutingKey,
                Version = metadata.Version,
                Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), MessagingJson.Options),
                OccurredAt = occurredAt,
                CorrelationId = CorrelationContext.Current,
                TraceParent = Activity.Current?.Id,
            });
        }
    }
}
