using AutoMarket.BuildingBlocks.Messaging;
using AutoMarket.BuildingBlocks.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMarket.BuildingBlocks.Audit;

public static class AuditServiceCollectionExtensions
{
    public const string QueueName = "audit.events";

    // Audit jurnalı: bütün modulların AuditRecorded event-lərini audit.audit_log-a yazır (ARCHITECTURE §3.2, SEC-LOG-03)
    public static IServiceCollection AddAudit(this IServiceCollection services)
    {
        services.AddModuleDbContext<AuditDbContext>(AuditDbContext.SchemaName);
        services.AddIntegrationEventConsumer<AuditDbContext>(QueueName, consumer => consumer
            .Handle<AuditRecorded, AuditRecordedConsumer>());

        return services;
    }
}
