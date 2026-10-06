using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using Microsoft.Extensions.Options;

namespace AutoMarket.Identity.Application.Auth;

// FR-AUTH-02 AC1/AC3: ≥ 128 bit təsadüfi token (256 bit), serverdə hash, 24 saat; yeni token köhnəsini etibarsız edir
internal sealed class EmailConfirmationIssuer(
    IOneTimeTokenRepository tokens,
    ISecureTokenGenerator tokenGenerator,
    IIdGenerator ids,
    IOptions<IdentityOptions> options)
{
    public async Task IssueAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        await tokens.RevokeActiveAsync(user.Id, OneTimeTokenPurpose.EmailConfirmation, now, cancellationToken);

        var rawToken = tokenGenerator.Generate();
        var token = OneTimeToken.Issue(
            ids.NewId(),
            user.Id,
            OneTimeTokenPurpose.EmailConfirmation,
            TokenHasher.Hash(rawToken),
            now,
            now + options.Value.Tokens.EmailConfirmationLifetime);
        tokens.Add(token);

        user.RequestEmailConfirmation(rawToken, token.ExpiresAt);
    }
}
