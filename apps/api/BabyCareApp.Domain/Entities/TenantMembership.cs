using BabyCareApp.Domain.Common;
using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Domain.Entities;

public class TenantMembership : IAuditableEntity, Common.ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public MembershipStatus Status { get; set; } = MembershipStatus.Invited;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
