using System.ComponentModel.DataAnnotations;

namespace BabyCareApp.Application.Admin;

public class UpdateSubscriptionPlanRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal MonthlyPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AnnualPrice { get; set; }

    public int? MaxLearners { get; set; }
    public int? MaxTeachers { get; set; }
    public int? MaxBranches { get; set; }

    public string[] FeatureFlags { get; set; } = [];

    public bool IsActive { get; set; } = true;
}
