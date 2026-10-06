namespace AutoMarket.Identity.Application.Abstractions;

// SEC-AUTH-01: uzunluq, sızmış şifrələr siyahısı, email/ad ilə müqayisə. Kompozisiya qaydası yoxdur
internal interface IPasswordPolicy
{
    public IReadOnlyList<PasswordPolicyViolation> Validate(string password, string? email, string? name);
}

internal sealed record PasswordPolicyViolation(string Code, string Message);
