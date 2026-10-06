namespace AutoMarket.Notifications.Application.Abstractions;

// NFR-ENV-02: göndərmə üsulu konfiqurasiya ilə dəyişir (lokal Mailpit, digər mühitlərdə real SMTP); testdə fake (NFR-TEST-06)
internal interface IEmailTransport
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

internal sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);
