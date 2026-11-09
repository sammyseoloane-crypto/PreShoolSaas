namespace BabyCareApp.Application.Tenancy;

public record BranchResponse(
    Guid Id,
    string Name,
    bool IsMainBranch,
    bool IsActive,
    string? AddressLine,
    string? City,
    string? Province,
    string? PostalCode,
    string? Country,
    string? ContactEmail,
    string? ContactPhone);
