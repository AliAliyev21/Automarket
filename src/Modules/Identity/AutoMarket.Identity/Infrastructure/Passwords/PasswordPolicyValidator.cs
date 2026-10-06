using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace AutoMarket.Identity.Infrastructure.Passwords;

// ADR-0003: Identity-nin IPasswordValidator genişlənmə nöqtəsi; qaydalar IPasswordPolicy-dədir (validator ilə eyni)
internal sealed class PasswordPolicyValidator(IPasswordPolicy policy) : IPasswordValidator<User>
{
    public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string? password)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrEmpty(password))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "PASSWORD_REQUIRED" }));
        }

        var violations = policy.Validate(password, user.Email, user.Name);
        var result = violations.Count == 0
            ? IdentityResult.Success
            : IdentityResult.Failed([.. violations.Select(violation => new IdentityError { Code = violation.Code, Description = violation.Message })]);

        return Task.FromResult(result);
    }
}
