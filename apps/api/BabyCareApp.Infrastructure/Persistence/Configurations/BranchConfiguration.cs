using BabyCareApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BabyCareApp.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.AddressLine).HasMaxLength(300);
        builder.Property(b => b.City).HasMaxLength(100);
        builder.Property(b => b.Province).HasMaxLength(100);
        builder.Property(b => b.PostalCode).HasMaxLength(20);
        builder.Property(b => b.Country).HasMaxLength(100);
        builder.Property(b => b.ContactEmail).HasMaxLength(320);
        builder.Property(b => b.ContactPhone).HasMaxLength(50);

        builder.HasOne(b => b.Tenant)
            .WithMany(t => t.Branches)
            .HasForeignKey(b => b.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.TenantId);
    }
}
