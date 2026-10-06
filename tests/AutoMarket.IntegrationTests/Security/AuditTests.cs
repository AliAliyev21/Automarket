using System.Text.Json;
using AutoMarket.IntegrationTests.Infrastructure;
using Npgsql;

namespace AutoMarket.IntegrationTests.Security;

// SEC-LOG-03/04, FR-AUTH-03 AC6: auth hadisələri outbox → RabbitMQ → audit.audit_log yolu ilə yazılır
public sealed class AuditTests(ContainersFixture containers)
{
    [Fact]
    public async Task Login_FailedAndSucceeded_AuditedWithoutPersonalData()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);
        using (await AuthApi.LoginAsync(client, session.Email, "Wrong-Password-123"))
        {
        }

        var userId = await GetUserIdAsync(client, session.AccessToken);
        var rows = await Eventually.WaitAsync(
            () => ReadAuditAsync(userId),
            entries => entries.Any(entry => entry.EventType == "auth.login_failed")
                && entries.Any(entry => entry.EventType == "auth.login_succeeded"),
            TestContext.Current.CancellationToken);

        var failed = rows.First(entry => entry.EventType == "auth.login_failed");
        failed.Result.ShouldBe("failure");
        failed.Details.ShouldNotBeNull().ShouldContain("invalid_password");
        failed.CorrelationId.ShouldNotBeNullOrEmpty();
        rows.ShouldContain(entry => entry.EventType == "auth.registered");
        rows.ShouldContain(entry => entry.EventType == "auth.email_confirmed");

        // SEC-LOG-04: şəxsi məlumat minimaldır — email və şifrə audit qeydində yoxdur
        rows.ShouldAllBe(entry => entry.Details == null || !entry.Details.Contains(session.Email, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AuditLog_UpdateAttempt_RejectedByTrigger()
    {
        await using var connection = new NpgsqlConnection(containers.PostgresConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand("UPDATE audit.audit_log SET result = 'tampered'", connection);

        // Cədvəl boş olsa belə trigger sətir səviyyəsindədir; əvvəlcə ən azı bir sətir olmasını təmin edirik
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        await AuthApi.RegisterConfirmAndLoginAsync(client);
        await Eventually.WaitAsync(() => CountAsync(connection), count => count > 0, TestContext.Current.CancellationToken);

        await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<Guid> GetUserIdAsync(HttpClient client, string accessToken)
    {
        using var me = await AuthApi.GetMeAsync(client, accessToken);
        using var body = JsonDocument.Parse(await me.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<IReadOnlyList<AuditRow>> ReadAuditAsync(Guid userId)
    {
        await using var connection = new NpgsqlConnection(containers.PostgresConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT event_type, result, details::text, correlation_id FROM audit.audit_log WHERE target_id = @target",
            connection);
        command.Parameters.AddWithValue("target", userId.ToString());

        var rows = new List<AuditRow>();
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            rows.Add(new AuditRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3)));
        }

        return rows;
    }

    private static async Task<long> CountAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("SELECT count(*) FROM audit.audit_log", connection);
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private sealed record AuditRow(string EventType, string Result, string? Details, string? CorrelationId);
}
