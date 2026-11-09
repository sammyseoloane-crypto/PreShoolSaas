using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace BabyCareApp.Tests.TestDoubles;

public class FakeHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "BabyCareApp.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}
