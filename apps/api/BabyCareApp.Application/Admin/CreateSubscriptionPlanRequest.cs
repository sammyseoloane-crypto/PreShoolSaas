using System.ComponentModel.DataAnnotations;
using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Application.Admin;

public class CreateSubscriptionPlanRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public SubscriptionTier Tier { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MonthlyPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AnnualPrice { get; set; }

    public int? MaxLearners { get; set; }
    public int? MaxTeachers { get; set; }
    public int? MaxBranches { get; set; }

    [Range(0, 365)]
    public int TrialDays { get; set; } = 14;

    public string[] FeatureFlags { get; set; } = [];
}
