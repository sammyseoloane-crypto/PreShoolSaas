using System.IdentityModel.Tokens.Jwt;
using BabyCareApp.Domain.Enums;
using BabyCareApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BabyCareApp.Api.Middleware;

/// <summary>
/// Reads the X-Tenant-Id header (when present), validates the authenticated caller has
/// an active membership in that tenant (DB-backed — never trusted from the header alone),
/// and stores the validated id in HttpContext.Items for HttpTenantContext to read. Absence
/// of the header simply leaves the tenant unresolved — not every endpoint is tenant-scoped.
/// </summary>
public class TenantResolutionMiddleware
{
    public const string ResolvedTenantIdKey = "ResolvedTenantId";
    private const string HeaderName = "X-Tenant-Id";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            if (!Guid.TryParse(headerValue, out var tenantId))
            {
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest, $"Invalid {HeaderName} header.");
                return;
            }

            var subClaim = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(subClaim, out var userId))
            {
                await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "No authenticated user.");
                return;
            }

            var hasActiveMembership = await db.TenantMemberships.AnyAsync(m =>
                m.TenantId == tenantId && m.UserId == userId && m.Status == MembershipStatus.Active);

            if (!hasActiveMembership)
            {
                await WriteErrorAsync(context, StatusCodes.Status403Forbidden, "You do not have access to this tenant.");
                return;
            }

            context.Items[ResolvedTenantIdKey] = tenantId;
        }

        await _next(context);
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new { status = statusCode, title = message });
    }
}
