using BabyCareApp.Domain.Common;

namespace BabyCareApp.Domain.Entities;

/// <summary>
/// BranchId is nullable: some compliance documents (company registration, tax clearance)
/// are organisation-wide, others (a specific branch's fire safety certificate) are
/// branch-specific. DocumentType is a free string rather than an enum — the product owner
/// hasn't specified a fixed taxonomy, and constraining it now would be inventing a business
/// rule rather than following one.
/// </summary>
public class SchoolDocument : ITenantOwnedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    /// <summary>Path within the Supabase Storage bucket — never a public URL directly.</summary>
    public string StorageKey { get; set; } = string.Empty;

    public Guid? UploadedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Branch? Branch { get; set; }
}
