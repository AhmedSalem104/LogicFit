using LogicFit.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace LogicFit.Infrastructure.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private readonly string[] _allowedVideoExtensions = { ".mp4", ".webm", ".mov", ".avi" };
    private const long MaxImageSize = 5 * 1024 * 1024; // 5MB
    private const long MaxVideoSize = 100 * 1024 * 1024; // 100MB

    public FileUploadService(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string? subfolder = null)
    {
        ValidateImage(file);
        return await SaveFileAsync(file, "images", subfolder);
    }

    public async Task<string> UploadVideoAsync(IFormFile file, string? subfolder = null)
    {
        ValidateVideo(file);
        return await SaveFileAsync(file, "videos", subfolder);
    }

    public async Task<List<string>> UploadImagesAsync(List<IFormFile> files, string? subfolder = null)
    {
        var urls = new List<string>();
        foreach (var file in files)
        {
            var url = await UploadImageAsync(file, subfolder);
            urls.Add(url);
        }
        return urls;
    }

    public Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl))
            return Task.FromResult(false);

        // Convert URL to file path
        var relativePath = fileUrl.Replace("/uploads/", "");
        var filePath = Path.Combine(_environment.WebRootPath, "uploads", relativePath);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public string GetFullUrl(string relativePath)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
            return relativePath;

        return $"{request.Scheme}://{request.Host}{relativePath}";
    }

    private async Task<string> SaveFileAsync(IFormFile file, string fileType, string? subfolder)
    {
        // Create path: wwwroot/uploads/{fileType}/{year}/{month}/{subfolder?}
        var now = DateTime.UtcNow;
        var year = now.Year.ToString();
        var month = now.Month.ToString("D2");

        var pathParts = new List<string> { _environment.WebRootPath, "uploads", fileType, year, month };
        if (!string.IsNullOrEmpty(subfolder))
        {
            pathParts.Add(subfolder);
        }

        var uploadPath = Path.Combine(pathParts.ToArray());

        // Ensure directory exists
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        // Generate unique filename
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadPath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative URL
        var relativePathParts = new List<string> { "/uploads", fileType, year, month };
        if (!string.IsNullOrEmpty(subfolder))
        {
            relativePathParts.Add(subfolder);
        }
        relativePathParts.Add(fileName);

        return string.Join("/", relativePathParts);
    }

    private void ValidateImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file provided");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedImageExtensions.Contains(extension))
            throw new ArgumentException($"Invalid image format. Allowed formats: {string.Join(", ", _allowedImageExtensions)}");

        if (file.Length > MaxImageSize)
            throw new ArgumentException($"Image size exceeds maximum allowed size of {MaxImageSize / 1024 / 1024}MB");
    }

    private void ValidateVideo(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file provided");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedVideoExtensions.Contains(extension))
            throw new ArgumentException($"Invalid video format. Allowed formats: {string.Join(", ", _allowedVideoExtensions)}");

        if (file.Length > MaxVideoSize)
            throw new ArgumentException($"Video size exceeds maximum allowed size of {MaxVideoSize / 1024 / 1024}MB");
    }
}
