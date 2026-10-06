using System.Text.Json;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Messaging;
using AutoMarket.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace AutoMarket.IntegrationTests.Messaging;

// ARCHITECTURE §5.3: at-least-once çatdırılmada dublikat mesaj inbox ilə ikinci effekt yaratmır
public sealed class InboxTests(ContainersFixture containers)
{
    [Fact]
    public async Task Process_SameMessageTwice_HandledOnce()
    {
        await using var factory = containers.CreateFactory();
        var host = factory.Services.GetServices<IHostedService>().OfType<RabbitMqConsumerHost<AuditDbContext>>().Single();
        var definition = factory.Services.GetRequiredService<ConsumerDefinition<AuditDbContext>>();

        var messageId = Guid.CreateVersion7();
        var auditEvent = new AuditRecorded("test.inbox", null, "test", messageId.ToString(), "success", null, null, null, factory.Time.GetUtcNow(), null);
        var routingKey = IntegrationEventMetadata.Of<AuditRecorded>().RoutingKey;
        var envelope = new MessageEnvelope(
            messageId,
            routingKey,
            1,
            factory.Time.GetUtcNow(),
            null,
            null,
            JsonSerializer.SerializeToElement(auditEvent, MessagingJson.Options));
        var registration = definition.Handlers[routingKey];

        await host.ProcessAsync(envelope, registration, auditEvent, TestContext.Current.CancellationToken);
        await host.ProcessAsync(envelope, registration, auditEvent, TestContext.Current.CancellationToken);

        (await CountAsync("SELECT count(*) FROM audit.audit_log WHERE id = @id", messageId)).ShouldBe(1);
        (await CountAsync("SELECT count(*) FROM audit.inbox WHERE message_id = @id", messageId)).ShouldBe(1);
    }

    private async Task<long> CountAsync(string sql, Guid id)
    {
        await using var connection = new NpgsqlConnection(containers.PostgresConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
