namespace BabyCareApp.Application.Admin;

public record PlatformStatisticsResponse(
    int TotalTenants,
    IReadOnlyDictionary<string, int> TenantsByStatus,
    int TotalUsers,
    int TotalBranches,
    int TotalActiveSubscriptions);
