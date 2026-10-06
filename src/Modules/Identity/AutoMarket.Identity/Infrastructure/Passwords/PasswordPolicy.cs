using System.Collections.Frozen;
using System.Globalization;
using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application;
using AutoMarket.Identity.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AutoMarket.Identity.Infrastructure.Passwords;

// SEC-AUTH-01 (NIST SP 800-63B): uzunluq Unicode code point-lərlə; kompozisiya qaydası yoxdur; sızmış şifrələr yalnız lokal
// siyahı ilə yoxlanılır (Q1: şifrə və ya hash-i xarici servisə göndərilmir); şifrə email-ə və ya ada bərabər ola və onu ehtiva edə bilməz
internal sealed class PasswordPolicy(IOptions<IdentityOptions> options) : IPasswordPolicy
{
    // Qısa ad (məs. "Al") demək olar hər şifrədə ola bilər; "ehtiva edir" yoxlaması bu uzunluqdan başlayır, bərabərlik isə həmişə yoxlanılır
    private const int MinContainedLength = 3;

    private const string ResourceName = "AutoMarket.Identity.CommonPasswords";

    private readonly FrozenSet<string> _commonPasswords = LoadCommonPasswords(options.Value.Password.MinLength);

    public IReadOnlyList<PasswordPolicyViolation> Validate(string password, string? email, string? name)
    {
        ArgumentNullException.ThrowIfNull(password);

        var settings = options.Value.Password;
        var violations = new List<PasswordPolicyViolation>();

        var length = password.EnumerateRunes().Count();
        if (length < settings.MinLength)
        {
            violations.Add(new(ValidationCodes.PasswordTooShort, $"Password must be at least {settings.MinLength} characters long."));
        }
        else if (length > settings.MaxLength)
        {
            violations.Add(new(ValidationCodes.PasswordTooLong, $"Password must be at most {settings.MaxLength} characters long."));
        }

        if (_commonPasswords.Contains(password))
        {
            violations.Add(new(ValidationCodes.PasswordTooCommon, "Password is too common."));
        }

        if (ContainsPersonalInfo(password, email, name))
        {
            violations.Add(new(ValidationCodes.PasswordContainsPersonalInfo, "Password must not contain your email or name."));
        }

        return violations;
    }

    private static bool ContainsPersonalInfo(string password, string? email, string? name)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(email))
        {
            var trimmed = email.Trim();
            candidates.Add(trimmed);

            var at = trimmed.IndexOf('@', StringComparison.Ordinal);
            if (at > 0)
            {
                candidates.Add(trimmed[..at]);
            }
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            candidates.Add(name.Trim());
        }

        var compareInfo = CultureInfo.InvariantCulture.CompareInfo;
        foreach (var candidate in candidates)
        {
            if (string.Equals(password, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (candidate.Length >= MinContainedLength && compareInfo.IndexOf(password, candidate, CompareOptions.IgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    // Minimum uzunluqdan qısa şifrələr onsuz da rədd olunur, ona görə yaddaşda saxlanılmır
    private static FrozenSet<string> LoadCommonPasswords(int minLength)
    {
        using var stream = typeof(PasswordPolicy).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource {ResourceName} is missing.");
        using var reader = new StreamReader(stream);

        var passwords = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            if (line.Length >= minLength)
            {
                passwords.Add(line);
            }
        }

        return passwords.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }
}
