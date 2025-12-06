using Microsoft.AspNetCore.Http;

namespace LogicFit.Application.Common.Interfaces;

public interface IFileUploadService
{
    Task<string> UploadImageAsync(IFormFile file, string? subfolder = null);
    Task<string> UploadVideoAsync(IFormFile file, string? subfolder = null);
    Task<List<string>> UploadImagesAsync(List<IFormFile> files, string? subfolder = null);
    Task<bool> DeleteFileAsync(string fileUrl);
    string GetFullUrl(string relativePath);
}
