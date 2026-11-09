using BabyCareApp.Domain.Common;
using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Domain.Entities;

/// <summary>
/// Platform-wide identity. Not tenant-owned — a user's relationship to any given
/// tenant is expressed through <see cref="TenantMembership"/>, since one person
/// (e.g. a parent) may be associated with more than one preschool.
/// </summary>
public class User : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsPlatformSuperAdmin { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<TenantMembership> Memberships { get; set; } = new List<TenantMembership>();
}
