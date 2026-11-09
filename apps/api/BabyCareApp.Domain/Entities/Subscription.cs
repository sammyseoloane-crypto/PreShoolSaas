using BabyCareApp.Domain.Common;
using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Domain.Entities;

/// <summary>
/// A tenant's SaaS subscription to the platform. Entirely separate from preschool
/// school-fee billing to parents (Slice 15) — never mix the two.
/// </summary>
public class Subscription : IAuditableEntity, Common.ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public DateTimeOffset? TrialEndsAt { get; set; }
    public DateTimeOffset CurrentPeriodStart { get; set; }
    public DateTimeOffset CurrentPeriodEnd { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}
