using AutoMarket.Notifications.Application.Abstractions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AutoMarket.Notifications.Infrastructure.Email;

// ARCHITECTURE §12: MailKit (System.Net.Mail.SmtpClient yeni kod üçün tövsiyə olunmur). Header-ləri MailKit öz encoding-i ilə
// yazır, istifadəçi inputu header-lərə yerləşdirilmir (SEC-INP-06). Uğursuz göndərmə exception-dır → consumer retry
internal sealed class MailKitEmailTransport(IOptions<SmtpOptions> options) : IEmailTransport
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var settings = options.Value;

        using var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.Timeout);

        using var client = new SmtpClient { Timeout = (int)settings.Timeout.TotalMilliseconds };
        await client.ConnectAsync(settings.Host, settings.Port, settings.Security, timeout.Token);

        if (!string.IsNullOrEmpty(settings.UserName))
        {
            await client.AuthenticateAsync(settings.UserName, settings.Password ?? string.Empty, timeout.Token);
        }

        await client.SendAsync(mime, timeout.Token);
        await client.DisconnectAsync(quit: true, timeout.Token);
    }
}
