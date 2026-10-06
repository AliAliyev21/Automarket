using System.Collections.Concurrent;
using AutoMarket.Notifications.Application.Abstractions;

namespace AutoMarket.IntegrationTests.Infrastructure;

// NFR-TEST-06: SMTP fake ilə əvəz olunur. Store statikdir: bir neçə factory eyni queue-dan oxuyur (competing consumers),
// mesajı hansı factory-nin consumer-i işlədəcəyi bilinmir. Testlər unikal email ünvanı ilə axtarır
internal sealed class FakeEmailTransport : IEmailTransport
{
    private static readonly ConcurrentQueue<EmailMessage> Sent = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        Sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public static IReadOnlyList<EmailMessage> SentTo(string email) =>
        [.. Sent.Where(message => string.Equals(message.To, email, StringComparison.OrdinalIgnoreCase))];

    // Sabit Task.Delay yoxdur: şərt ödənənə qədər qısa intervalla yoxlanılır, timeout ilə (ARCHITECTURE §10.1)
    public static async Task<IReadOnlyList<EmailMessage>> WaitForAsync(string email, int count, CancellationToken cancellationToken)
    {
        var messages = await Eventually.WaitAsync(
            () => Task.FromResult(SentTo(email)),
            messages => messages.Count >= count,
            cancellationToken);

        return messages;
    }
}
