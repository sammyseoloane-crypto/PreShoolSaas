using System.ComponentModel.DataAnnotations;

namespace BabyCareApp.Application.Auth;

public class UpdateProfileRequest
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
}
