using System.ComponentModel.DataAnnotations;

namespace BabyCareApp.Application.Tenancy;

public class UpdateBranchRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
