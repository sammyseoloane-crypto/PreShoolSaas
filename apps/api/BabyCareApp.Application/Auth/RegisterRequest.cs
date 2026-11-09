using System.ComponentModel.DataAnnotations;

namespace BabyCareApp.Application.Auth;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(200)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
}
