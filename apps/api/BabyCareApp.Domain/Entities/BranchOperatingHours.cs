using BabyCareApp.Domain.Common;

namespace BabyCareApp.Domain.Entities;

/// <summary>One row per day of the week for a branch — a full week is 7 rows.</summary>
public class BranchOperatingHours : ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan? OpenTime { get; set; }
    public TimeSpan? CloseTime { get; set; }
    public bool IsClosed { get; set; }

    public Branch Branch { get; set; } = null!;
}
