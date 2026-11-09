using System.ComponentModel.DataAnnotations;

namespace BabyCareApp.Application.Tenancy;

public class UpdateTenantRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [EmailAddress, MaxLength(320)]
    public string? ContactEmail { get; set; }

    [MaxLength(50)]
    public string? ContactPhone { get; set; }

    [Url, MaxLength(500)]
    public string? Website { get; set; }
}
