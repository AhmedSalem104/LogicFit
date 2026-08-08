using Amazon.S3;
using Amazon.S3.Model;
using LogicFit.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

/// <summary>Cloudflare R2 implementation of the shared upload contract.</summary>
/// <remarks>Keys are tenant-scoped so an object can never be reused across gyms by accident.</remarks>
public sealed class R2FileUploadService : IFileUploadService
{
    private static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private static readonly HashSet<string> DocumentExtensions = [".jpg", ".jpeg", ".png", ".pdf"];
    private static readonly HashSet<string> VideoExtensions = [".mp4", ".webm", ".mov", ".avi"];
    private readonly IAmazonS3 _s3;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<R2FileUploadService> _logger;
    private readonly string _bucket;

    public R2FileUploadService(IAmazonS3 s3, IConfiguration configuration, IHttpContextAccessor http, ILogger<R2FileUploadService> logger)
    {
        _s3 = s3;
        _configuration = configuration;
        _http = http;
        _logger = logger;
        _bucket = configuration["Storage:R2:Bucket"] ?? throw new InvalidOperationException("Storage:R2:Bucket is missing.");
    }

    public Task<string> UploadImageAsync(IFormFile file, string? subfolder = null) => UploadAsync(file, "images", ImageExtensions, 5 * 1024 * 1024, subfolder);
    public Task<string> UploadDocumentAsync(IFormFile file, string? subfolder = null) => UploadAsync(file, "documents", DocumentExtensions, 10 * 1024 * 1024, subfolder);
    public Task<string> UploadVideoAsync(IFormFile file, string? subfolder = null) => UploadAsync(file, "videos", VideoExtensions, 100 * 1024 * 1024, subfolder);

    public async Task<List<string>> UploadImagesAsync(List<IFormFile> files, string? subfolder = null)
    {
        var result = new List<string>(files.Count);
        foreach (var file in files) result.Add(await UploadImageAsync(file, subfolder));
        return result;
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        var key = ExtractKey(fileUrl);
        if (key is null) return false;
        await _s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = key });
        return true;
    }

    public string GetFullUrl(string relativePath)
    {
        if (Uri.TryCreate(relativePath, UriKind.Absolute, out _)) return relativePath;
        var publicBase = _configuration["Storage:R2:PublicBaseUrl"]?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(publicBase) && relativePath.StartsWith("/r2/", StringComparison.OrdinalIgnoreCase))
            return $"{publicBase}/{relativePath[5..]}";
        var request = _http.HttpContext?.Request;
        return request is null ? relativePath : $"{request.Scheme}://{request.Host}{relativePath}";
    }

    private async Task<string> UploadAsync(IFormFile file, string type, HashSet<string> extensions, long maxBytes, string? subfolder)
    {
        if (file is null || file.Length == 0) throw new ArgumentException("No file provided");
        if (file.Length > maxBytes) throw new ArgumentException($"File size exceeds maximum allowed size of {maxBytes / 1024 / 1024}MB");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!extensions.Contains(extension)) throw new ArgumentException("File format is not allowed");
        var contentTypeAllowed = type switch
        {
            "images" => file.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true,
            "videos" => file.ContentType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true,
            "documents" => file.ContentType is "image/jpeg" or "image/png" or "application/pdf",
            _ => false
        };
        if (!string.IsNullOrWhiteSpace(file.ContentType) && !contentTypeAllowed)
            throw new ArgumentException("File content type is not allowed");
        if (!string.IsNullOrWhiteSpace(subfolder) && subfolder.Split('/', '\\').Any(x => x is "." or "..")) throw new ArgumentException("Invalid upload subfolder");

        var tenant = _http.HttpContext?.User?.FindFirst("TenantId")?.Value;
        var scope = Guid.TryParse(tenant, out var id) ? id.ToString("N") : "platform";
        var key = $"tenants/{scope}/{type}/{DateTime.UtcNow:yyyy/MM}/{(string.IsNullOrWhiteSpace(subfolder) ? "" : subfolder.Trim('/') + "/")}{Guid.NewGuid():N}{extension}";
        await using var stream = file.OpenReadStream();
        await _s3.PutObjectAsync(new PutObjectRequest { BucketName = _bucket, Key = key, InputStream = stream, ContentType = file.ContentType ?? "application/octet-stream", AutoCloseStream = false });
        _logger.LogInformation("Stored media object {Key}", key);
        var publicBase = _configuration["Storage:R2:PublicBaseUrl"]?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(publicBase) || IsPrivateCollection(subfolder)
            ? $"/api/media/object?key={Uri.EscapeDataString(key)}"
            : $"{publicBase}/{key}";
    }

    private static bool IsPrivateCollection(string? subfolder)
        => string.Equals(subfolder, "payment-proofs", StringComparison.OrdinalIgnoreCase)
           || string.Equals(subfolder, "measurements", StringComparison.OrdinalIgnoreCase)
           || string.Equals(subfolder, "identity-documents", StringComparison.OrdinalIgnoreCase);

    private string? ExtractKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Query.Contains("key=")) value = uri.Query.Split("key=", 2)[1];
        if (value.StartsWith("/api/media/object?key=", StringComparison.OrdinalIgnoreCase)) value = value[22..];
        return Uri.UnescapeDataString(value).Replace('\\', '/').Contains("..", StringComparison.Ordinal) ? null : Uri.UnescapeDataString(value).TrimStart('/');
    }
}
