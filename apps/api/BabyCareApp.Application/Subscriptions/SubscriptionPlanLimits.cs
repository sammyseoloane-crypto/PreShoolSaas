namespace BabyCareApp.Application.Subscriptions;

/// <summary>Null from GetEffectiveLimitsAsync means no Trialing/Active subscription is in
/// effect — callers should treat that as "no plan-based limit applies" (see
/// ISubscriptionService for why), not as "zero allowed".</summary>
public record SubscriptionPlanLimits(int? MaxLearners, int? MaxTeachers, int? MaxBranches);
