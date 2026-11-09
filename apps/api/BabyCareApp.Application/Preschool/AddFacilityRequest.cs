using System.ComponentModel.DataAnnotations;

namespace BabyCareApp.Application.Preschool;

public class AddFacilityRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}
