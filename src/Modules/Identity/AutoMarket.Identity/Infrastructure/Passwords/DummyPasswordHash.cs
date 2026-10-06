using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace AutoMarket.Identity.Infrastructure.Passwords;

// SEC-AUTH-08: hesab olmadıqda real hash ilə eyni parametrli dummy hash yoxlanılır ki, cavab müddəti hesabın varlığını açmasın.
// Hash prosesdə bir dəfə, cari iterasiya sayı ilə yaradılır
internal sealed class DummyPasswordHash(ISecureTokenGenerator tokenGenerator)
{
    private static readonly User DummyUser = User.Register(Guid.Empty, "dummy@invalid", "dummy", null, DateTimeOffset.UnixEpoch);

    private readonly Lock _lock = new();
    private string? _hash;

    public void Verify(IPasswordHasher<User> hasher, string password)
    {
        ArgumentNullException.ThrowIfNull(hasher);

        _ = hasher.VerifyHashedPassword(DummyUser, GetHash(hasher), password);
    }

    private string GetHash(IPasswordHasher<User> hasher)
    {
        lock (_lock)
        {
            return _hash ??= hasher.HashPassword(DummyUser, tokenGenerator.Generate());
        }
    }
}
