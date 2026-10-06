using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Application.Auth.Register;
using AutoMarket.Identity.Domain.Users;
using FluentValidation;

namespace AutoMarket.Identity.Application.Admin.BootstrapAdmin;

// ARCHITECTURE §11, R-05: ilk Admin yalnız birdəfəlik CLI əmri ilə yaradılır (HTTP endpoint-i yoxdur, kodda parol yoxdur).
// Sistemdə aktiv Admin varsa əmr rədd olunur. Email mövcud deyilsə təsdiqlənmiş yeni hesab yaradılır (şifrə SEC-AUTH-01
// siyasəti ilə yoxlanılır), mövcud aktiv hesaba isə yalnız Admin rolu verilir
internal sealed class BootstrapAdminHandler(
    IUserRepository users,
    IPasswordPolicy passwordPolicy,
    IIdentityUnitOfWork unitOfWork,
    IAuditLog audit,
    IIdGenerator ids,
    TimeProvider time) : ICommandHandler<BootstrapAdminCommand, BootstrapAdminOutcome>
{
    private static readonly InlineValidator<string> EmailValidator = CreateEmailValidator();

    public async Task<Result<BootstrapAdminOutcome>> HandleAsync(BootstrapAdminCommand command, CancellationToken cancellationToken)
    {
        if (await users.AnyActiveAdminAsync(cancellationToken))
        {
            return CommonErrors.Forbidden with { Message = "An active administrator already exists." };
        }

        if (!(await EmailValidator.ValidateAsync(command.Email, cancellationToken)).IsValid)
        {
            return CommonErrors.ValidationFailedFor("email", [new FieldError(ValidationCodes.InvalidFormat, "Email is invalid.")]);
        }

        var email = EmailAddress.Normalize(command.Email);
        var now = time.GetUtcNow();
        var outcome = BootstrapAdminOutcome.Promoted;

        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            if (PasswordPolicyErrors.Check(passwordPolicy, command.Password, email, command.Name) is { } policyError)
            {
                return policyError;
            }

            user = User.RegisterConfirmed(ids.NewId(), email, command.Name, now);
            await users.CreateAsync(user, command.Password, cancellationToken);
            outcome = BootstrapAdminOutcome.Created;
        }
        else if (user.Status != UserStatus.Active)
        {
            return CommonErrors.Forbidden with { Message = "The existing account must be confirmed and not blocked." };
        }

        var roles = outcome == BootstrapAdminOutcome.Created ? [Roles.User] : await users.GetRolesAsync(user.Id, cancellationToken);
        await users.AddRoleAsync(user.Id, Roles.Admin, cancellationToken);
        user.RecordRolesChanged([.. roles.Append(Roles.Admin).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]);

        audit.Record(new AuditEntry(
            AdminAuditEvents.AdminBootstrapped,
            AuthAuditEvents.ResultSuccess,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: user.Id.ToString()));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return outcome;
    }

    private static InlineValidator<string> CreateEmailValidator()
    {
        var validator = new InlineValidator<string>();
        validator.RuleFor(email => email).NotEmpty().MaximumLength(RegisterRequestValidator.EmailMaxLength).EmailAddress();
        return validator;
    }
}
