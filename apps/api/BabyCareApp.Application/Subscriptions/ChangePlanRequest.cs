using System.ComponentModel.DataAnnotations;
using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Application.Subscriptions;

public class ChangePlanRequest
{
    [Required]
    public Guid PlanId { get; set; }

    [Required]
    public BillingCycle BillingCycle { get; set; }
}
