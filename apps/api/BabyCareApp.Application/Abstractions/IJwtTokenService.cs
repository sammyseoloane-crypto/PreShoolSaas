using BabyCareApp.Domain.Entities;

namespace BabyCareApp.Application.Abstractions;

public record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface IJwtTokenService
{
    AccessToken CreateAccessToken(User user);

    /// <summary>A cryptographically random opaque value — never a JWT. Only its hash is persisted.</summary>
    string GenerateRefreshTokenValue();

    TimeSpan RefreshTokenLifetime { get; }
}
