namespace BabyCareApp.Application.Tenancy;

public record BranchResponse(Guid Id, string Name, bool IsMainBranch, bool IsActive);
