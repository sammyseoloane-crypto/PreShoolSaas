using BabyCareApp.Application.Abstractions;
using BabyCareApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace BabyCareApp.Infrastructure.Auth;

/// <summary>
/// Reuses ASP.NET Core Identity's PBKDF2 hasher as a library only — this project does
/// not use the full Identity framework (IdentityUser/IdentityRole), since that schema
/// would collide with the tenant-aware User/Role/TenantMembership model (Slice 2).
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string hash, string password) =>
        _inner.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}
