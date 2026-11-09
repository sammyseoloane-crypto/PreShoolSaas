namespace BabyCareApp.Domain.Entities;

/// <summary>
/// A role catalog entry. <see cref="TenantId"/> is null for the fixed system roles
/// (Platform Super Admin, Preschool Owner, School Administrator, Teacher, Finance
/// Officer, Parent/Guardian); a non-null value scopes a custom role to one tenant.
/// </summary>
public class Role
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<TenantMembership> Memberships { get; set; } = new List<TenantMembership>();
}
