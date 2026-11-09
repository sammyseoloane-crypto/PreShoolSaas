namespace BabyCareApp.Application.Auth;

public record UserProfileResponse(Guid Id, string Email, string FullName, bool IsPlatformSuperAdmin);
