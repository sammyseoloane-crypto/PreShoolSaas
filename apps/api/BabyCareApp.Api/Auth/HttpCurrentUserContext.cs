using System.IdentityModel.Tokens.Jwt;
using BabyCareApp.Application.Abstractions;

namespace BabyCareApp.Api.Auth;

public class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var subClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(subClaim, out var userId) ? userId : null;
        }
    }

    public bool IsPlatformSuperAdmin =>
        _httpContextAccessor.HttpContext?.User.FindFirst("is_platform_super_admin")?.Value == bool.TrueString;
}
