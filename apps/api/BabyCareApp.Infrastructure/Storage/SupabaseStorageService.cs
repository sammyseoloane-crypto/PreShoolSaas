using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BabyCareApp.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace BabyCareApp.Infrastructure.Storage;

/// <summary>
/// Talks to Supabase Storage's REST API directly over HttpClient — Supabase doesn't ship
/// a first-party .NET Storage client, and the API surface needed here (upload/sign/delete)
/// is small enough that a hand-rolled client is simpler than pulling in a community package.
/// </summary>
public class SupabaseStorageService : IFileStorageService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseStorageOptions _options;

    public SupabaseStorageService(HttpClient httpClient, IOptions<SupabaseStorageOptions> options)
    {
        _options = options.Value;

        httpClient.BaseAddress = new Uri($"{_options.ProjectUrl.TrimEnd('/')}/storage/v1/");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
        httpClient.DefaultRequestHeaders.Add("apikey", _options.ServiceRoleKey);
        _httpClient = httpClient;
    }

    public async Task<UploadedFile> UploadAsync(
        string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var response = await _httpClient.PostAsync(
            $"object/{_options.Bucket}/{storageKey}", streamContent, cancellationToken);
        response.EnsureSuccessStatusCode();

        return new UploadedFile(storageKey, content.Length);
    }

    public async Task<string> GetSignedDownloadUrlAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"object/sign/{_options.Bucket}/{storageKey}",
            new SignRequest(_options.SignedUrlExpirySeconds),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<SignResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Supabase Storage returned an empty sign response.");

        return $"{_options.ProjectUrl.TrimEnd('/')}/storage/v1{body.SignedURL}";
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"object/{_options.Bucket}/{storageKey}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private record SignRequest([property: JsonPropertyName("expiresIn")] int ExpiresIn);

    private record SignResponse([property: JsonPropertyName("signedURL")] string SignedURL);
}
