using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Application.Admin;

public record SubscriptionResponse(
    Guid Id,
    Guid TenantId,
    string TenantName,
    Guid PlanId,
    string PlanName,
    SubscriptionStatus Status,
    BillingCycle BillingCycle,
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd);
