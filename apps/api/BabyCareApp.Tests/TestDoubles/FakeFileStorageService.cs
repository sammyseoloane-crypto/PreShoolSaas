using BabyCareApp.Application.Abstractions;

namespace BabyCareApp.Tests.TestDoubles;

public class FakeFileStorageService : IFileStorageService
{
    public List<string> UploadedKeys { get; } = [];
    public List<string> DeletedKeys { get; } = [];

    public Task<UploadedFile> UploadAsync(
        string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        UploadedKeys.Add(storageKey);
        return Task.FromResult(new UploadedFile(storageKey, content.Length));
    }

    public Task<string> GetSignedDownloadUrlAsync(string storageKey, CancellationToken cancellationToken = default) =>
        Task.FromResult($"https://fake-storage.test/{storageKey}?signed=1");

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        DeletedKeys.Add(storageKey);
        return Task.CompletedTask;
    }
}
