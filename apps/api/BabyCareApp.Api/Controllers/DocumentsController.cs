using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Exceptions;
using BabyCareApp.Application.Preschool;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

/// <summary>School documents — tenant-wide (BranchId omitted) or scoped to a specific
/// branch. Files are stored in Supabase Storage (see IFileStorageService); this controller
/// only ever handles metadata and signed-URL issuance, never raw storage credentials.</summary>
[ApiController]
[Route("api/v1/tenants/current/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IPreschoolProfileService _preschoolProfileService;
    private readonly ICurrentUserContext _currentUserContext;

    public DocumentsController(IPreschoolProfileService preschoolProfileService, ICurrentUserContext currentUserContext)
    {
        _preschoolProfileService = preschoolProfileService;
        _currentUserContext = currentUserContext;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:documents.read")]
    public async Task<ActionResult<IReadOnlyList<SchoolDocumentResponse>>> List(CancellationToken cancellationToken)
    {
        var documents = await _preschoolProfileService.ListDocumentsAsync(cancellationToken);
        return Ok(documents);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:documents.write")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<SchoolDocumentResponse>> Upload(
        [FromForm] UploadDocumentForm form, CancellationToken cancellationToken)
    {
        if (form.File.Length == 0)
        {
            throw new ValidationException("No file was uploaded.");
        }

        await using var stream = form.File.OpenReadStream();
        var document = await _preschoolProfileService.UploadDocumentAsync(
            form.BranchId, form.DocumentType, form.File.FileName, form.File.ContentType, stream,
            _currentUserContext.UserId, cancellationToken);

        return Ok(document);
    }

    [HttpGet("{id:guid}/download-url")]
    [Authorize(Policy = "Permission:documents.read")]
    public async Task<ActionResult<string>> GetDownloadUrl(Guid id, CancellationToken cancellationToken)
    {
        var url = await _preschoolProfileService.GetDocumentDownloadUrlAsync(id, cancellationToken);
        return Ok(new { url });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:documents.write")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _preschoolProfileService.DeleteDocumentAsync(id, cancellationToken);
        return NoContent();
    }
}
