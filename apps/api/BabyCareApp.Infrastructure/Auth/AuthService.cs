using System.Security.Cryptography;
using System.Text;
using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Auth;
using BabyCareApp.Application.Exceptions;
using BabyCareApp.Domain.Entities;
using BabyCareApp.Domain.Enums;
using BabyCareApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BabyCareApp.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IHostEnvironment environment,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _environment = environment;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            throw new ConflictException("An account with this email address already exists.");
        }

        var user = new User
        {
            Email = normalizedEmail,
            FullName = request.FullName.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = UserStatus.Active,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new AuthenticationException("This account is suspended or deactivated.");
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(string refreshTokenValue, CancellationToken cancellationToken = default)
    {
        var tokenHash = Hash(refreshTokenValue);
        var token = await _db.Set<RefreshToken>()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token is not null && token.RevokedAt is null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<AuthResult> RefreshAsync(string refreshTokenValue, CancellationToken cancellationToken = default)
    {
        var tokenHash = Hash(refreshTokenValue);
        var token = await _db.Set<RefreshToken>()
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token is null)
        {
            throw new AuthenticationException("Invalid refresh token.");
        }

        if (token.RevokedAt is not null)
        {
            // Reuse of an already-rotated or already-revoked token — treat as compromised
            // and revoke every active session for this user.
            var activeTokens = await _db.Set<RefreshToken>()
                .Where(t => t.UserId == token.UserId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;
            foreach (var active in activeTokens)
            {
                active.RevokedAt = now;
            }

            await _db.SaveChangesAsync(cancellationToken);
            throw new AuthenticationException("This refresh token has already been used. All sessions have been revoked.");
        }

        if (token.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new AuthenticationException("This refresh token has expired.");
        }

        var result = await IssueTokensAsync(token.User, cancellationToken, replacing: token);
        return result;
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Always behave the same whether or not the account exists, to prevent enumeration.
        if (user is null)
        {
            return;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTimeOffset.UtcNow.Add(PasswordResetTokenLifetime),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Set<PasswordResetToken>().Add(resetToken);
        await _db.SaveChangesAsync(cancellationToken);

        // Real delivery (email) arrives in Slice 18. Logged only in Development, and only
        // here, so the token is never exposed via the API response.
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "Password reset token for {Email}: {Token} (expires {ExpiresAt})",
                user.Email, rawToken, resetToken.ExpiresAt);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = Hash(request.Token);
        var resetToken = await _db.Set<PasswordResetToken>()
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null || !resetToken.IsActive)
        {
            throw new AuthenticationException("This password reset link is invalid or has expired.");
        }

        resetToken.User.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        resetToken.UsedAt = DateTimeOffset.UtcNow;

        var activeRefreshTokens = await _db.Set<RefreshToken>()
            .Where(t => t.UserId == resetToken.UserId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var active in activeRefreshTokens)
        {
            active.RevokedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([userId], cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return ToProfileResponse(user);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(
        Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([userId], cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.FullName = request.FullName.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        return ToProfileResponse(user);
    }

    private async Task<AuthResult> IssueTokensAsync(
        User user, CancellationToken cancellationToken, RefreshToken? replacing = null)
    {
        var accessToken = _jwtTokenService.CreateAccessToken(user);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshTokenValue();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(refreshTokenValue),
            ExpiresAt = DateTimeOffset.UtcNow.Add(_jwtTokenService.RefreshTokenLifetime),
        };

        _db.Set<RefreshToken>().Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        if (replacing is not null)
        {
            replacing.RevokedAt = DateTimeOffset.UtcNow;
            replacing.ReplacedByTokenId = refreshToken.Id;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new AuthResult(accessToken.Value, accessToken.ExpiresAt, refreshTokenValue);
    }

    private static UserProfileResponse ToProfileResponse(User user) =>
        new(user.Id, user.Email, user.FullName, user.IsPlatformSuperAdmin);

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(bytes);
    }
}
