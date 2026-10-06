using Npgsql;

namespace AutoMarket.IntegrationTests.Infrastructure;

// Test hazırlığı və yoxlama üçün birbaşa SQL: Admin rolunun verilməsi (ilk Admin CLI ilə yaradılır, testlərdə isə hər test öz
// Admin-ini yaradır), outbox və audit sətirlərinin oxunması
internal static class IdentityDatabase
{
    // RoleSeed id-ləri (migration-da sabitdir)
    public static readonly Guid ModeratorRoleId = Guid.Parse("0199b8a0-0000-7000-8000-000000000002");
    public static readonly Guid AdminRoleId = Guid.Parse("0199b8a0-0000-7000-8000-000000000003");

    public static async Task GrantRoleAsync(string connectionString, Guid userId, Guid roleId)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = new NpgsqlCommand(
            "INSERT INTO identity.user_roles (user_id, role_id) VALUES (@user, @role) ON CONFLICT DO NOTHING",
            connection);
        command.Parameters.AddWithValue("user", userId);
        command.Parameters.AddWithValue("role", roleId);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    // type — routing key (məs. identity.user.blocked.v1)
    public static async Task<long> CountOutboxAsync(string connectionString, string type, Guid userId)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM identity.outbox WHERE type = @type AND payload->>'userId' = @user",
            connection);
        command.Parameters.AddWithValue("type", type);
        command.Parameters.AddWithValue("user", userId.ToString());
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    public static async Task<IReadOnlyList<string>> ReadAuditEventTypesAsync(string connectionString, Guid targetUserId)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = new NpgsqlCommand("SELECT event_type FROM audit.audit_log WHERE target_id = @target", connection);
        command.Parameters.AddWithValue("target", targetUserId.ToString());

        var types = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            types.Add(reader.GetString(0));
        }

        return types;
    }

    private static async Task<NpgsqlConnection> OpenAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        return connection;
    }
}
