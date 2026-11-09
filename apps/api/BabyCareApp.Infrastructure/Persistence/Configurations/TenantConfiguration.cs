using BabyCareApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BabyCareApp.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Slug).IsUnique();

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(30);

        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.Property(t => t.ContactEmail).HasMaxLength(320);
        builder.Property(t => t.ContactPhone).HasMaxLength(50);
        builder.Property(t => t.Website).HasMaxLength(500);
        builder.Property(t => t.LogoUrl).HasMaxLength(500);
    }
}
