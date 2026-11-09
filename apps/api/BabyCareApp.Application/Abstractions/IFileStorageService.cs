namespace BabyCareApp.Application.Abstractions;

public record UploadedFile(string StorageKey, long SizeBytes);

/// <summary>
/// Backed by Supabase Storage (SupabaseStorageService) — the confirmed choice for this
/// project. Requires Supabase Storage credentials (project URL + service role key) via
/// configuration, same as the Postgres connection string: real code, pending secrets.
/// </summary>
public interface IFileStorageService
{
    Task<UploadedFile> UploadAsync(
        string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>A short-lived signed URL — documents live in a private bucket, never public.</summary>
    Task<string> GetSignedDownloadUrlAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
