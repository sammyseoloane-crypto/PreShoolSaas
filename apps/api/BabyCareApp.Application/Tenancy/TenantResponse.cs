using BabyCareApp.Domain.Enums;

namespace BabyCareApp.Application.Tenancy;

public record TenantResponse(
    Guid Id,
    string Name,
    string Slug,
    TenantStatus Status,
    string? Description,
    string? ContactEmail,
    string? ContactPhone,
    string? Website,
    string? LogoUrl);
