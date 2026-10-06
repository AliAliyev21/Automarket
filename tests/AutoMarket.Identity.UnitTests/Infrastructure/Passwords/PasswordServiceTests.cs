using System.Buffers.Binary;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Users;
using AutoMarket.Identity.Infrastructure.Passwords;
using AutoMarket.Identity.UnitTests.Builders;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AutoMarket.Identity.UnitTests.Infrastructure.Passwords;

// SEC-AUTH-02, ADR-0003: standart PasswordHasher V3, PBKDF2-HMAC-SHA512, iterasiya ≥ 210 000, login zamanı rehash
public sealed class PasswordServiceTests
{
    private const string Password = "x7!kq2#z9-strong";

    [Fact]
    public void HashPassword_ConfiguredIterations_StoredInV3Header()
    {
        var hasher = Hasher(210_000);
        var user = new UserBuilder().Build();

        var hash = Convert.FromBase64String(hasher.HashPassword(user, Password));

        hash[0].ShouldBe((byte)0x01);                                          // V3 format
        BinaryPrimitives.ReadUInt32BigEndian(hash.AsSpan(1)).ShouldBe(2u);     // HMACSHA512
        BinaryPrimitives.ReadUInt32BigEndian(hash.AsSpan(5)).ShouldBe(210_000u);
    }

    [Fact]
    public void Verify_CorrectPassword_Success()
    {
        var hasher = Hasher(210_000);
        var user = new UserBuilder().Build();
        user.PasswordHash = hasher.HashPassword(user, Password);

        Service(hasher).Verify(user, Password).ShouldBe(PasswordCheckResult.Success);
    }

    [Fact]
    public void Verify_WrongPassword_Failed()
    {
        var hasher = Hasher(210_000);
        var user = new UserBuilder().Build();
        user.PasswordHash = hasher.HashPassword(user, Password);

        Service(hasher).Verify(user, "wrong-password!").ShouldBe(PasswordCheckResult.Failed);
    }

    [Fact]
    public void Verify_OlderIterationCount_RehashNeededAndRehashUpgrades()
    {
        var user = new UserBuilder().Build();
        user.PasswordHash = Hasher(100_000).HashPassword(user, Password);
        var service = Service(Hasher(210_000));

        service.Verify(user, Password).ShouldBe(PasswordCheckResult.SuccessRehashNeeded);

        service.Rehash(user, Password);
        service.Verify(user, Password).ShouldBe(PasswordCheckResult.Success);
    }

    private static PasswordHasher<User> Hasher(int iterations) =>
        new(Options.Create(new PasswordHasherOptions { IterationCount = iterations }));

    private static PasswordService Service(PasswordHasher<User> hasher)
    {
        var generator = Substitute.For<ISecureTokenGenerator>();
        generator.Generate().Returns("dummy-password-value");
        return new PasswordService(hasher, new DummyPasswordHash(generator));
    }
}
