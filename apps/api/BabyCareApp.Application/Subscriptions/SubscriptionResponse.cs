using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Application.Subscriptions;

public record SubscriptionResponse(
    Guid Id,
    Guid PlanId,
    string PlanName,
    SubscriptionStatus Status,
    BillingCycle BillingCycle,
    DateTimeOffset? TrialEndsAt,
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd,
    DateTimeOffset? CancelledAt);
