using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Application.Admin;

public record SubscriptionPlanResponse(
    Guid Id,
    string Name,
    SubscriptionTier Tier,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    int? MaxLearners,
    int? MaxTeachers,
    int? MaxBranches,
    string[] FeatureFlags,
    bool IsActive);
