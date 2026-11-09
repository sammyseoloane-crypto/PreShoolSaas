using BabyCareApp.Application.Abstractions;

namespace BabyCareApp.Tests.TestDoubles;

public class FakeTenantContext : ITenantContext
{
    public Guid? TenantId { get; set; }
}
