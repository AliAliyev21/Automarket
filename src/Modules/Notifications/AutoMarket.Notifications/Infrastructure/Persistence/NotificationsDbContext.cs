using AutoMarket.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoMarket.Notifications.Infrastructure.Persistence;

// Schema notifications. Mərhələ 3a-da yalnız inbox (və boş outbox) var; notifications, email_queue və s. sonrakı mərhələdədir
internal sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "notifications";

    protected override string Schema => SchemaName;
}

internal sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args) =>
        new(ModuleDatabaseOptions.ForDesignTime<NotificationsDbContext>(NotificationsDbContext.SchemaName));
}
