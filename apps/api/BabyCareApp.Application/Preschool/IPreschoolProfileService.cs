namespace BabyCareApp.Application.Preschool;

public interface IPreschoolProfileService
{
    Task<IReadOnlyList<OperatingHourEntry>> GetOperatingHoursAsync(Guid branchId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the whole week in one call — matches how a weekly-schedule editor
    /// typically saves, and avoids the ambiguity of partial per-day updates.</summary>
    Task<IReadOnlyList<OperatingHourEntry>> SetOperatingHoursAsync(
        Guid branchId, UpdateOperatingHoursRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FacilityResponse>> ListFacilitiesAsync(Guid branchId, CancellationToken cancellationToken = default);

    Task<FacilityResponse> AddFacilityAsync(
        Guid branchId, AddFacilityRequest request, CancellationToken cancellationToken = default);

    Task RemoveFacilityAsync(Guid branchId, Guid facilityId, CancellationToken cancellationToken = default);

    /// <summary>Lists documents across the whole tenant (both tenant-wide and every branch's own).</summary>
    Task<IReadOnlyList<SchoolDocumentResponse>> ListDocumentsAsync(CancellationToken cancellationToken = default);

    Task<SchoolDocumentResponse> UploadDocumentAsync(
        Guid? branchId,
        string documentType,
        string fileName,
        string contentType,
        Stream content,
        Guid? uploadedByUserId,
        CancellationToken cancellationToken = default);

    Task<string> GetDocumentDownloadUrlAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
