using BabyCareApp.Application.Abstractions;

namespace BabyCareApp.Tests.TestDoubles;

public class FakeCurrentUserContext : ICurrentUserContext
{
    public Guid? UserId { get; set; }
    public bool IsPlatformSuperAdmin { get; set; }
}
