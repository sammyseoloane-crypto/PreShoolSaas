using System.ComponentModel.DataAnnotations;

namespace BabyCareApp.Application.Auth;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
