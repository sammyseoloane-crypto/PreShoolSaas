namespace BabyCareApp.Domain.Entities;

/// <summary>
/// Platform-wide, tied to a User rather than a tenant. Only the hash is stored;
/// the raw token value is only ever known to the client that received it.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public User User { get; set; } = null!;

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
