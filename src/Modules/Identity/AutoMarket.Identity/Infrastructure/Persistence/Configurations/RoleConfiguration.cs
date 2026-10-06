using AutoMarket.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoMarket.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.Property(x => x.Name).HasMaxLength(32);
        builder.Property(x => x.NormalizedName).HasMaxLength(32);
        builder.Property(x => x.ConcurrencyStamp).HasMaxLength(64);
        builder.HasIndex(x => x.NormalizedName).HasDatabaseName("ix_roles_normalized_name").IsUnique();

        builder.HasData(RoleSeed.All);
    }
}
