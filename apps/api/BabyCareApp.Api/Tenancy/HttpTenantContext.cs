using BabyCareApp.Api.Middleware;
using BabyCareApp.Application.Abstractions;

namespace BabyCareApp.Api.Tenancy;

public class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId =>
        _httpContextAccessor.HttpContext?.Items.TryGetValue(TenantResolutionMiddleware.ResolvedTenantIdKey, out var value) == true
            ? value as Guid?
            : null;
}
