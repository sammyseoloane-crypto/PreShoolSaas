using BabyCareApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BabyCareApp.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Key).IsRequired().HasMaxLength(150);
        builder.HasIndex(p => p.Key).IsUnique();

        builder.Property(p => p.Description).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Category).HasMaxLength(100);
    }
}
