using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AutoMarket.BuildingBlocks.Persistence;

public static class ModuleDatabaseOptions
{
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    // snake_case, migration tarixçəsi modulun öz schema-sında (ARCHITECTURE §4.2, CONVENTIONS §2.6)
    public static DbContextOptionsBuilder UseModuleDatabase(this DbContextOptionsBuilder builder, NpgsqlDataSource dataSource, string schema)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .UseNpgsql(dataSource, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable, schema))
            .UseSnakeCaseNamingConvention();
    }

    // Design-time (dotnet ef) üçün: migration yaradılarkən bazaya qoşulmur
    public static DbContextOptions<TContext> ForDesignTime<TContext>(string schema)
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        builder
            .UseNpgsql("Host=localhost;Database=automarket_design", npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable, schema))
            .UseSnakeCaseNamingConvention();

        return builder.Options;
    }
}
