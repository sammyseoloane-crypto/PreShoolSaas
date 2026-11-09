using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Auth;
using BabyCareApp.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserContext _currentUserContext;

    public UsersController(IAuthService authService, ICurrentUserContext currentUserContext)
    {
        _authService = authService;
        _currentUserContext = currentUserContext;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        var profile = await _authService.GetProfileAsync(userId, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyProfile(
        UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        var profile = await _authService.UpdateProfileAsync(userId, request, cancellationToken);
        return Ok(profile);
    }

    private Guid RequireCurrentUserId() =>
        _currentUserContext.UserId ?? throw new AuthenticationException("No authenticated user resolved from the access token.");
}
