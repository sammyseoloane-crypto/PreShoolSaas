using System.ComponentModel.DataAnnotations;

namespace BabyCareApp.Application.Tenancy;

public class RegisterTenantRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional — auto-generated from Name (with a numeric suffix on collision) if omitted.</summary>
    [MaxLength(100)]
    public string? Slug { get; set; }
}
