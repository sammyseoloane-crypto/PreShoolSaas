using System.ComponentModel.DataAnnotations;

namespace BabyCareApp.Application.Auth;

public class ResetPasswordRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(200)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
