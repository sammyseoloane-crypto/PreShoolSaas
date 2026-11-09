using BabyCareApp.Domain.Common;

namespace BabyCareApp.Domain.Entities;

public class Branch : IAuditableEntity, Common.ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsMainBranch { get; set; }
    public bool IsActive { get; set; } = true;

    // Branch settings — physical location and contact details for this specific branch.
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; } = "South Africa";
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<BranchOperatingHours> OperatingHours { get; set; } = new List<BranchOperatingHours>();
    public ICollection<BranchFacility> Facilities { get; set; } = new List<BranchFacility>();
}
