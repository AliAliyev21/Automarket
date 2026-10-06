using AutoMarket.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoMarket.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.NormalizedEmail).HasMaxLength(256);
        builder.Property(x => x.UserName).HasMaxLength(256);
        builder.Property(x => x.NormalizedUserName).HasMaxLength(256);
        builder.Property(x => x.PasswordHash).HasMaxLength(512);
        builder.Property(x => x.SecurityStamp).HasMaxLength(64);
        builder.Property(x => x.ConcurrencyStamp).HasMaxLength(64);
        builder.Property(x => x.PhoneNumber).HasMaxLength(13);
        builder.Property(x => x.Name).HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

        // FR-AUTH-01 AC2: email unikaldır (normallaşdırılmış sütun üzrə)
        builder.HasIndex(x => x.NormalizedEmail).HasDatabaseName("ix_users_normalized_email").IsUnique();
        builder.HasIndex(x => x.NormalizedUserName).HasDatabaseName("ix_users_normalized_user_name").IsUnique();

        builder.Ignore(x => x.DomainEvents);
    }
}
