using BabyCareApp.Application.Auth;
using BabyCareApp.Application.Exceptions;
using BabyCareApp.Infrastructure.Auth;
using BabyCareApp.Infrastructure.Persistence;
using BabyCareApp.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BabyCareApp.Tests.Auth;

public class AuthServiceTests
{
    private static (AuthService Service, ApplicationDbContext Db) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new ApplicationDbContext(options, new FakeTenantContext());
        db.Database.EnsureCreated(); // applies HasData seed rows — not automatic on InMemory

        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = Convert.ToBase64String(new byte[64]),
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30,
        });

        var service = new AuthService(
            db,
            new PasswordHasher(),
            new JwtTokenService(jwtOptions),
            new FakeHostEnvironment(),
            NullLogger<AuthService>.Instance);

        return (service, db);
    }

    private static RegisterRequest ValidRegisterRequest(string email = "parent@example.com") => new()
    {
        Email = email,
        Password = "CorrectHorseBattery1",
        ConfirmPassword = "CorrectHorseBattery1",
        FullName = "Jane Parent",
    };

    [Fact]
    public async Task Register_NewEmail_Succeeds_AndReturnsTokens()
    {
        var (service, _) = CreateService();

        var result = await service.RegisterAsync(ValidRegisterRequest());

        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.True(result.AccessTokenExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        var (service, _) = CreateService();
        await service.RegisterAsync(ValidRegisterRequest());

        await Assert.ThrowsAsync<ConflictException>(
            () => service.RegisterAsync(ValidRegisterRequest()));
    }

    [Fact]
    public async Task Register_EmailIsNormalizedToLowercase()
    {
        var (service, db) = CreateService();
        await service.RegisterAsync(ValidRegisterRequest("Parent@Example.com"));

        var user = await db.Users.SingleAsync();
        Assert.Equal("parent@example.com", user.Email);
    }

    [Fact]
    public async Task Login_CorrectCredentials_Succeeds()
    {
        var (service, _) = CreateService();
        await service.RegisterAsync(ValidRegisterRequest());

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "parent@example.com",
            Password = "CorrectHorseBattery1",
        });

        Assert.NotEmpty(result.AccessToken);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsAuthenticationException()
    {
        var (service, _) = CreateService();
        await service.RegisterAsync(ValidRegisterRequest());

        await Assert.ThrowsAsync<AuthenticationException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "parent@example.com",
            Password = "WrongPassword1",
        }));
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsAuthenticationException()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<AuthenticationException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "nobody@example.com",
            Password = "whatever",
        }));
    }

    [Fact]
    public async Task Refresh_ValidToken_RotatesAndReturnsNewTokens()
    {
        var (service, _) = CreateService();
        var registerResult = await service.RegisterAsync(ValidRegisterRequest());

        var refreshed = await service.RefreshAsync(registerResult.RefreshToken);

        Assert.NotEqual(registerResult.RefreshToken, refreshed.RefreshToken);
        Assert.NotEqual(registerResult.AccessToken, refreshed.AccessToken);
    }

    [Fact]
    public async Task Refresh_ReusedRevokedToken_RevokesAllSessions_AndThrows()
    {
        var (service, db) = CreateService();
        var registerResult = await service.RegisterAsync(ValidRegisterRequest());

        // First use rotates the token (the original becomes revoked).
        await service.RefreshAsync(registerResult.RefreshToken);

        // Reusing the now-revoked original token must be treated as compromised.
        await Assert.ThrowsAsync<AuthenticationException>(
            () => service.RefreshAsync(registerResult.RefreshToken));

        var allTokensForUser = await db.RefreshTokens.ToListAsync();
        Assert.All(allTokensForUser, t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task Refresh_UnknownToken_ThrowsAuthenticationException()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<AuthenticationException>(
            () => service.RefreshAsync("not-a-real-token"));
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var (service, _) = CreateService();
        var registerResult = await service.RegisterAsync(ValidRegisterRequest());

        await service.LogoutAsync(registerResult.RefreshToken);

        await Assert.ThrowsAsync<AuthenticationException>(
            () => service.RefreshAsync(registerResult.RefreshToken));
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_DoesNotThrow()
    {
        var (service, _) = CreateService();

        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "nobody@example.com" });
        // No exception, and no observable difference from the known-email path.
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Throws()
    {
        var (service, _) = CreateService();
        await service.RegisterAsync(ValidRegisterRequest());

        await Assert.ThrowsAsync<AuthenticationException>(() => service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = "invalid-token",
            NewPassword = "NewPassword1",
            ConfirmNewPassword = "NewPassword1",
        }));
    }

    [Fact]
    public async Task GetProfile_ReturnsExpectedFields()
    {
        var (service, db) = CreateService();
        await service.RegisterAsync(ValidRegisterRequest());
        var userId = (await db.Users.SingleAsync()).Id;

        var profile = await service.GetProfileAsync(userId);

        Assert.Equal("parent@example.com", profile.Email);
        Assert.Equal("Jane Parent", profile.FullName);
    }

    [Fact]
    public async Task UpdateProfile_ChangesFullName()
    {
        var (service, db) = CreateService();
        await service.RegisterAsync(ValidRegisterRequest());
        var userId = (await db.Users.SingleAsync()).Id;

        var updated = await service.UpdateProfileAsync(userId, new UpdateProfileRequest { FullName = "Jane Updated" });

        Assert.Equal("Jane Updated", updated.FullName);
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_CreatesOneActiveResetToken()
    {
        var (service, db) = CreateService();
        await service.RegisterAsync(ValidRegisterRequest());

        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "parent@example.com" });

        Assert.Equal(1, await db.PasswordResetTokens.CountAsync());
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ChangesPassword_AndRevokesExistingSessions()
    {
        // ForgotPasswordAsync never returns the raw token (by design — real delivery is
        // out-of-band email in Slice 18), so this test plants a reset token row directly,
        // using the same hash the service computes internally, to exercise the full
        // ResetPasswordAsync contract end to end.
        var (service, db) = CreateService();
        var registerResult = await service.RegisterAsync(ValidRegisterRequest());
        var user = await db.Users.SingleAsync();

        const string rawResetToken = "test-raw-reset-token";
        db.PasswordResetTokens.Add(new Domain.Entities.PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = ComputeHash(rawResetToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = rawResetToken,
            NewPassword = "BrandNewPassword1",
            ConfirmNewPassword = "BrandNewPassword1",
        });

        await Assert.ThrowsAsync<AuthenticationException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "parent@example.com",
            Password = "CorrectHorseBattery1",
        }));

        var loginResult = await service.LoginAsync(new LoginRequest
        {
            Email = "parent@example.com",
            Password = "BrandNewPassword1",
        });
        Assert.NotEmpty(loginResult.AccessToken);

        await Assert.ThrowsAsync<AuthenticationException>(
            () => service.RefreshAsync(registerResult.RefreshToken));
    }

    private static string ComputeHash(string value)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(bytes);
    }
}
