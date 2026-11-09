using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Application.Admin;

public class TenantListQuery
{
    public TenantStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
