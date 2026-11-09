using System.ComponentModel.DataAnnotations;

namespace BabyCareApp.Application.Auth;

public class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
