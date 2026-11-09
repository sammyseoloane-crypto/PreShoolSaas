using System.ComponentModel.DataAnnotations;

namespace BabyCareApp.Application.Tenancy;

public class CreateBranchRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
