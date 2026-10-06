using AutoMarket.BuildingBlocks.Security;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AutoMarket.Api.Configuration;

// Connection string yalnız user-secrets (Development) və ya environment variable ilə verilir (SEC-SEC-02)
internal sealed class PostgresOptions
{
    public const string SectionName = "Postgres";

    public string ConnectionString { get; set; } = string.Empty;
}

// Xəta mesajlarında konfiqurasiya dəyəri yazılmır (SEC-LOG-01)
internal sealed class PostgresOptionsValidator : IValidateOptions<PostgresOptions>
{
    private const string Key = $"{PostgresOptions.SectionName}:{nameof(PostgresOptions.ConnectionString)}";

    public ValidateOptionsResult Validate(string? name, PostgresOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail($"{Key} is required.");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(options.ConnectionString);
        }
        catch (ArgumentException)
        {
            return ValidateOptionsResult.Fail($"{Key} is not a valid PostgreSQL connection string.");
        }

        if (string.IsNullOrEmpty(builder.Host) || string.IsNullOrEmpty(builder.Database) || string.IsNullOrEmpty(builder.Username))
        {
            return ValidateOptionsResult.Fail($"{Key} must contain Host, Database and Username.");
        }

        if (string.IsNullOrEmpty(builder.Password) || SecretValue.IsPlaceholder(builder.Password))
        {
            return ValidateOptionsResult.Fail($"{Key} must contain a non-placeholder password.");
        }

        return ValidateOptionsResult.Success;
    }
}
