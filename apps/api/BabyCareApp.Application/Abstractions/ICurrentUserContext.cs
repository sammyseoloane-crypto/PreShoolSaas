namespace BabyCareApp.Application.Abstractions;

/// <summary>Resolved from the validated JWT's claims by the Api layer.</summary>
public interface ICurrentUserContext
{
    Guid? UserId { get; }

    bool IsPlatformSuperAdmin { get; }
}
