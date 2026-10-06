using System.Security.Cryptography;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoMarket.Identity.Infrastructure.Persistence.Configurations;

// ARCHITECTURE §4.5: token_hash unikal, (user_id, revoked_at), family_id
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash).HasMaxLength(SHA256.HashSizeInBytes);
        builder.Property(x => x.RevokedReason).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.CreatedIp).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.RevokedAt });
        builder.HasIndex(x => x.FamilyId);

        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_refresh_tokens_users_user_id");
        builder.Ignore(x => x.DomainEvents);
    }
}
