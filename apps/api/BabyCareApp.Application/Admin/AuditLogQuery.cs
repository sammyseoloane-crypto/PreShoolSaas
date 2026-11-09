namespace BabyCareApp.Application.Admin;

public class AuditLogQuery
{
    public Guid? TenantId { get; set; }
    public string? Action { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
