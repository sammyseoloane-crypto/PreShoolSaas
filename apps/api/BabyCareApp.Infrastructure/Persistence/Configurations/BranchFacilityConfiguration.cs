using BabyCareApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BabyCareApp.Infrastructure.Persistence.Configurations;

public class BranchFacilityConfiguration : IEntityTypeConfiguration<BranchFacility>
{
    public void Configure(EntityTypeBuilder<BranchFacility> builder)
    {
        builder.ToTable("branch_facilities");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(f => f.Name).IsRequired().HasMaxLength(200);
        builder.Property(f => f.Description).HasMaxLength(1000);

        builder.HasOne(f => f.Branch)
            .WithMany(b => b.Facilities)
            .HasForeignKey(f => f.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.TenantId);
        builder.HasIndex(f => f.BranchId);
    }
}
