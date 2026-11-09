using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

public class UploadDocumentForm
{
    [FromForm]
    public string DocumentType { get; set; } = string.Empty;

    [FromForm]
    public Guid? BranchId { get; set; }

    [FromForm]
    public IFormFile File { get; set; } = null!;
}
