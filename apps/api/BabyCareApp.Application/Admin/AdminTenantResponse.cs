using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Application.Admin;

public record AdminTenantResponse(
    Guid Id, string Name, string Slug, TenantStatus Status, DateTimeOffset CreatedAt, int BranchCount, int MembershipCount);
