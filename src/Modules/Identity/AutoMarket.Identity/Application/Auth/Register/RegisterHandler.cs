using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Users;

namespace AutoMarket.Identity.Application.Auth.Register;

// FR-AUTH-01. SEC-AUTH-08: email mövcud olsa da, olmasa da nəticə eynidir (202) və hər iki yolda bir şifrə hash-i
// hesablanır ki, cavab müddəti bərabər olsun. Məktublar outbox ilə asinxron gedir
internal sealed class RegisterHandler(
    IUserRepository users,
    IPasswordService passwords,
    EmailConfirmationIssuer emailConfirmation,
    IIdentityUnitOfWork unitOfWork,
    IAuditLog audit,
    IIdGenerator ids,
    TimeProvider time) : ICommandHandler<RegisterCommand>
{
    public async Task<Result> HandleAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var email = EmailAddress.Normalize(command.Email);

        var existing = await users.FindByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            passwords.SimulateVerification(command.Password);

            // FR-AUTH-01 AC4: mövcud hesabın sahibinə xəbərdarlıq məktubu
            existing.RecordRegistrationAttempt();
            audit.Record(new AuditEntry(
                AuthAuditEvents.RegistrationAttemptOnExistingAccount,
                AuthAuditEvents.ResultFailure,
                TargetType: AuthAuditEvents.TargetUser,
                TargetId: existing.Id.ToString()));

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var user = User.Register(ids.NewId(), email, command.Name, command.Phone, now);
        await users.CreateAsync(user, command.Password, cancellationToken);
        await emailConfirmation.IssueAsync(user, now, cancellationToken);

        audit.Record(new AuditEntry(
            AuthAuditEvents.UserRegistered,
            AuthAuditEvents.ResultSuccess,
            ActorId: user.Id,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: user.Id.ToString()));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
