using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Exceptions;
using BabyCareApp.Application.Preschool;
using BabyCareApp.Application.Tenancy;
using BabyCareApp.Domain.Entities;
using BabyCareApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BabyCareApp.Infrastructure.Preschool;

public class PreschoolProfileService : IPreschoolProfileService
{
    private const long MaxDocumentSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantService _tenantService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditLogger _auditLogger;

    public PreschoolProfileService(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        ITenantService tenantService,
        IFileStorageService fileStorageService,
        IAuditLogger auditLogger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _tenantService = tenantService;
        _fileStorageService = fileStorageService;
        _auditLogger = auditLogger;
    }

    public async Task<IReadOnlyList<OperatingHourEntry>> GetOperatingHoursAsync(
        Guid branchId, CancellationToken cancellationToken = default)
    {
        await _tenantService.GetBranchAsync(branchId, cancellationToken); // ownership check

        var hours = await _db.BranchOperatingHours
            .Where(h => h.BranchId == branchId)
            .ToListAsync(cancellationToken);

        return hours.Select(ToEntry).OrderBy(e => e.DayOfWeek).ToList();
    }

    public async Task<IReadOnlyList<OperatingHourEntry>> SetOperatingHoursAsync(
        Guid branchId, UpdateOperatingHoursRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        await _tenantService.GetBranchAsync(branchId, cancellationToken); // ownership check

        var allDays = Enum.GetValues<DayOfWeek>();
        if (request.Days.Count != allDays.Length || request.Days.Select(d => d.DayOfWeek).Distinct().Count() != allDays.Length)
        {
            throw new ValidationException("Operating hours must include every day of the week exactly once.");
        }

        foreach (var day in request.Days)
        {
            if (!day.IsClosed && (day.OpenTime is null || day.CloseTime is null || day.OpenTime >= day.CloseTime))
            {
                throw new ValidationException(
                    $"{day.DayOfWeek}: open time must be before close time when the branch isn't closed that day.");
            }
        }

        var existing = await _db.BranchOperatingHours.Where(h => h.BranchId == branchId).ToListAsync(cancellationToken);
        _db.BranchOperatingHours.RemoveRange(existing);

        var newRows = request.Days.Select(day => new BranchOperatingHours
        {
            TenantId = tenantId,
            BranchId = branchId,
            DayOfWeek = day.DayOfWeek,
            OpenTime = day.IsClosed ? null : day.OpenTime,
            CloseTime = day.IsClosed ? null : day.CloseTime,
            IsClosed = day.IsClosed,
        }).ToList();

        _db.BranchOperatingHours.AddRange(newRows);
        await _db.SaveChangesAsync(cancellationToken);

        return newRows.Select(ToEntry).OrderBy(e => e.DayOfWeek).ToList();
    }

    public async Task<IReadOnlyList<FacilityResponse>> ListFacilitiesAsync(
        Guid branchId, CancellationToken cancellationToken = default)
    {
        await _tenantService.GetBranchAsync(branchId, cancellationToken); // ownership check

        var facilities = await _db.BranchFacilities
            .Where(f => f.BranchId == branchId)
            .ToListAsync(cancellationToken);

        return facilities.Select(ToFacilityResponse).ToList();
    }

    public async Task<FacilityResponse> AddFacilityAsync(
        Guid branchId, AddFacilityRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        await _tenantService.GetBranchAsync(branchId, cancellationToken); // ownership check

        var facility = new BranchFacility
        {
            TenantId = tenantId,
            BranchId = branchId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
        };

        _db.BranchFacilities.Add(facility);
        await _db.SaveChangesAsync(cancellationToken);

        return ToFacilityResponse(facility);
    }

    public async Task RemoveFacilityAsync(Guid branchId, Guid facilityId, CancellationToken cancellationToken = default)
    {
        await _tenantService.GetBranchAsync(branchId, cancellationToken); // ownership check

        var facility = await _db.BranchFacilities
            .SingleOrDefaultAsync(f => f.Id == facilityId && f.BranchId == branchId, cancellationToken)
            ?? throw new NotFoundException("Facility not found.");

        _db.BranchFacilities.Remove(facility);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SchoolDocumentResponse>> ListDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var documents = await _db.SchoolDocuments
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(ToDocumentResponse).ToList();
    }

    public async Task<SchoolDocumentResponse> UploadDocumentAsync(
        Guid? branchId,
        string documentType,
        string fileName,
        string contentType,
        Stream content,
        Guid? uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        if (content.Length > MaxDocumentSizeBytes)
        {
            throw new ValidationException($"Documents must be {MaxDocumentSizeBytes / (1024 * 1024)} MB or smaller.");
        }

        if (branchId is Guid resolvedBranchId)
        {
            await _tenantService.GetBranchAsync(resolvedBranchId, cancellationToken); // ownership check
        }

        var documentId = Guid.NewGuid();
        var storageKey = $"{tenantId}/{(branchId is Guid b ? b.ToString() : "tenant")}/{documentId}-{fileName}";

        var uploaded = await _fileStorageService.UploadAsync(storageKey, content, contentType, cancellationToken);

        var document = new SchoolDocument
        {
            Id = documentId,
            TenantId = tenantId,
            BranchId = branchId,
            DocumentType = documentType.Trim(),
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = uploaded.SizeBytes,
            StorageKey = storageKey,
            UploadedByUserId = uploadedByUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.SchoolDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "school_document.uploaded", nameof(SchoolDocument), document.Id, tenantId, uploadedByUserId,
            metadata: new { documentType, fileName }, cancellationToken);

        return ToDocumentResponse(document);
    }

    public async Task<string> GetDocumentDownloadUrlAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await FindOwnedDocumentAsync(documentId, cancellationToken);
        return await _fileStorageService.GetSignedDownloadUrlAsync(document.StorageKey, cancellationToken);
    }

    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await FindOwnedDocumentAsync(documentId, cancellationToken);

        await _fileStorageService.DeleteAsync(document.StorageKey, cancellationToken);
        _db.SchoolDocuments.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "school_document.deleted", nameof(SchoolDocument), document.Id, document.TenantId, userId: null,
            cancellationToken: cancellationToken);
    }

    private async Task<SchoolDocument> FindOwnedDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();

        return await _db.SchoolDocuments.SingleOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Document not found.");
    }

    private Guid RequireTenantId() =>
        _tenantContext.TenantId ?? throw new TenantRequiredException(
            "This operation requires a resolved tenant — send a valid X-Tenant-Id header for a tenant you belong to.");

    private static OperatingHourEntry ToEntry(BranchOperatingHours hours) =>
        new(hours.DayOfWeek, hours.OpenTime, hours.CloseTime, hours.IsClosed);

    private static FacilityResponse ToFacilityResponse(BranchFacility facility) =>
        new(facility.Id, facility.Name, facility.Description);

    private static SchoolDocumentResponse ToDocumentResponse(SchoolDocument document) => new(
        document.Id, document.BranchId, document.DocumentType, document.FileName,
        document.ContentType, document.SizeBytes, document.CreatedAt);
}
