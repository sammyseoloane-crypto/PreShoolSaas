using BabyCareApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BabyCareApp.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Tier).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.MonthlyPrice).HasColumnType("numeric(10,2)");
        builder.Property(p => p.AnnualPrice).HasColumnType("numeric(10,2)");
        builder.Property(p => p.FeatureFlags).HasColumnType("text[]");
    }
}
