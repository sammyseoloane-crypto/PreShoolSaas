namespace BabyCareApp.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(string refreshTokenValue, CancellationToken cancellationToken = default);

    Task<AuthResult> RefreshAsync(string refreshTokenValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Always completes without revealing whether the email exists, to prevent account
    /// enumeration. The reset token itself is only ever delivered by email (Slice 18) —
    /// in Development it is logged instead, never returned in the response.
    /// </summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<UserProfileResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
