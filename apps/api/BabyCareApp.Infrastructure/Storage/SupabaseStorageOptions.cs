namespace BabyCareApp.Infrastructure.Storage;

public class SupabaseStorageOptions
{
    public const string SectionName = "SupabaseStorage";

    /// <summary>e.g. https://qkgvojqzobeeyrzncwjs.supabase.co</summary>
    public string ProjectUrl { get; set; } = string.Empty;

    /// <summary>Set via `dotnet user-secrets` — never committed to the repo. Must be the
    /// service role key (not the anon key) — uploads/deletes need to bypass Storage RLS,
    /// since tenant isolation for documents is already enforced at the application layer
    /// (SchoolDocument rows carry TenantId and go through the same RLS as everything else).</summary>
    public string ServiceRoleKey { get; set; } = string.Empty;

    public string Bucket { get; set; } = "school-documents";

    public int SignedUrlExpirySeconds { get; set; } = 300;
}
