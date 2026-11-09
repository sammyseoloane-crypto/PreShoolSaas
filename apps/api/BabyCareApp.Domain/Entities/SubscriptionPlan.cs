using BabyCareApp.Domain.Common;
using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Domain.Entities;

/// <summary>Global plan catalog, managed by the platform owner — not tenant-owned.</summary>
public class SubscriptionPlan : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int? MaxLearners { get; set; }
    public int? MaxTeachers { get; set; }
    public int? MaxBranches { get; set; }
    public int TrialDays { get; set; } = 14;
    public string[] FeatureFlags { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
