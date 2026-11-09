using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Application.Tenancy;

/// <summary>One row of the current user's "my tenants" list — e.g. for a tenant switcher UI.</summary>
public record TenantSummaryResponse(Guid Id, string Name, string Slug, TenantStatus Status, string RoleName);
