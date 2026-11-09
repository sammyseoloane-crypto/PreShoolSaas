using BabyCareApp.Domain.Common;

namespace BabyCareApp.Domain.Entities;

public class BranchFacility : ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Branch Branch { get; set; } = null!;
}
