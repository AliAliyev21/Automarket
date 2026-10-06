using System.Text.Encodings.Web;
using AutoMarket.Identity.Contracts.Events;
using AutoMarket.Notifications.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AutoMarket.Notifications.Application.AuthEmails;

// NFR-ENV-05: hər məktub əvvəl az, sonra en mətni ehtiva edir, platformanın adı var, şəxsi məlumat minimumdur.
// SEC-INP-06: şablona daxil edilən dəyərlər HTML-encode olunur, subject sabit şablondur
internal sealed class AuthEmailRenderer(IOptions<NotificationsOptions> options)
{
    private const string Platform = "AutoMarket";

    public EmailMessage Render(string to, AuthEmailKind kind, string? token) => kind switch
    {
        AuthEmailKind.ConfirmEmail => ConfirmEmail(to, token ?? throw new InvalidOperationException("Confirmation email requires a token.")),
        AuthEmailKind.RegistrationAttemptOnExistingAccount => RegistrationAttempt(to),
        AuthEmailKind.LockedOut => LockedOut(to),
        _ => throw new NotSupportedException($"Auth email kind {kind} is not supported yet."),
    };

    private EmailMessage ConfirmEmail(string to, string token)
    {
        var link = $"{options.Value.Links.ConfirmEmailUrl}?token={Uri.EscapeDataString(token)}";

        return Build(
            to,
            "Email ünvanınızı təsdiqləyin / Confirm your email",
            [
                "AutoMarket-də qeydiyyatı tamamlamaq üçün email ünvanınızı təsdiqləyin. Link 24 saat etibarlıdır.",
                "Əgər qeydiyyatdan siz keçməmisinizsə, bu məktubu nəzərə almayın.",
            ],
            [
                "Confirm your email address to complete your AutoMarket registration. The link is valid for 24 hours.",
                "If you did not sign up, please ignore this email.",
            ],
            link);
    }

    private static EmailMessage RegistrationAttempt(string to) => Build(
        to,
        "Hesabınızla qeydiyyat cəhdi / Registration attempt with your account",
        [
            "Kimsə bu email ünvanı ilə AutoMarket-də yenidən qeydiyyatdan keçməyə çalışdı. Hesabınız artıq mövcuddur.",
            "Əgər bu siz deyildinizsə, heç nə etməyiniz lazım deyil.",
        ],
        [
            "Someone tried to sign up for AutoMarket with this email address. Your account already exists.",
            "If this wasn't you, no action is needed.",
        ],
        link: null);

    private static EmailMessage LockedOut(string to) => Build(
        to,
        "Hesabınız müvəqqəti kilidləndi / Your account was temporarily locked",
        [
            "Bir neçə uğursuz giriş cəhdindən sonra AutoMarket hesabınız müvəqqəti kilidləndi.",
            "Əgər bu siz deyildinizsə, şifrənizi dəyişməyiniz tövsiyə olunur.",
        ],
        [
            "Your AutoMarket account was temporarily locked after several failed sign-in attempts.",
            "If this wasn't you, we recommend changing your password.",
        ],
        link: null);

    private static EmailMessage Build(string to, string subject, string[] azParagraphs, string[] enParagraphs, string? link)
    {
        var html = HtmlEncoder.Default;

        var htmlBody = string.Concat(
            $"<p><strong>{html.Encode(Platform)}</strong></p>",
            string.Concat(azParagraphs.Select(p => $"<p>{html.Encode(p)}</p>")),
            link is null ? string.Empty : $"<p><a href=\"{html.Encode(link)}\">Təsdiqlə / Confirm</a></p>",
            "<hr>",
            string.Concat(enParagraphs.Select(p => $"<p>{html.Encode(p)}</p>")));

        var textBody = string.Join(
            Environment.NewLine + Environment.NewLine,
            [Platform, .. azParagraphs, .. link is null ? Array.Empty<string>() : [link], "---", .. enParagraphs]);

        return new EmailMessage(to, $"{Platform}: {subject}", htmlBody, textBody);
    }
}
