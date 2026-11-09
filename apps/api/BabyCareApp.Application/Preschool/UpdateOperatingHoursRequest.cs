using System.ComponentModel.DataAnnotations;

namespace BabyCareApp.Application.Preschool;

public class UpdateOperatingHoursRequest
{
    /// <summary>Must cover all 7 days exactly once — validated in the service (a cross-item
    /// rule like this doesn't fit a per-field data-annotation attribute).</summary>
    [Required]
    public List<OperatingHourEntry> Days { get; set; } = [];
}
