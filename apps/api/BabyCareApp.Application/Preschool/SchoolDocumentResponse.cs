namespace BabyCareApp.Application.Preschool;

public record SchoolDocumentResponse(
    Guid Id,
    Guid? BranchId,
    string DocumentType,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt);
