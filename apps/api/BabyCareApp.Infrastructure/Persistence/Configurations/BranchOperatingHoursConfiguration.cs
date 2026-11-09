using BabyCareApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BabyCareApp.Infrastructure.Persistence.Configurations;

public class BranchOperatingHoursConfiguration : IEntityTypeConfiguration<BranchOperatingHours>
{
    public void Configure(EntityTypeBuilder<BranchOperatingHours> builder)
    {
        builder.ToTable("branch_operating_hours");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(h => h.DayOfWeek).HasConversion<string>().HasMaxLength(10);

        builder.HasOne(h => h.Branch)
            .WithMany(b => b.OperatingHours)
            .HasForeignKey(h => h.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.TenantId);
        builder.HasIndex(h => new { h.BranchId, h.DayOfWeek }).IsUnique();
    }
}
