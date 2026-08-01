using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Media;

/// <summary>Streams private media through the API instead of exposing sensitive files from wwwroot.</summary>
[ApiController]
[Route("api/media")]
[Authorize]
public sealed class MediaController : ControllerBase
{
    private readonly IAmazonS3? _s3;
    private readonly IConfiguration _configuration;

    public MediaController(IServiceProvider services, IConfiguration configuration)
    {
        _s3 = services.GetService<IAmazonS3>();
        _configuration = configuration;
    }

    [HttpGet("object")]
    public async Task<IActionResult> GetObject([FromQuery] string key, CancellationToken cancellationToken)
    {
        if (_s3 is null || string.IsNullOrWhiteSpace(key) || key.Contains("..", StringComparison.Ordinal) || key.Contains('\\'))
            return NotFound();

        key = Uri.UnescapeDataString(key).TrimStart('/');
        var tenantId = User.FindFirst("TenantId")?.Value;
        var allowedPrefix = Guid.TryParse(tenantId, out var id) ? $"tenants/{id:N}/" : "platform/";
        if (!key.StartsWith(allowedPrefix, StringComparison.OrdinalIgnoreCase)) return Forbid();

        try
        {
            var response = await _s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _configuration["Storage:R2:Bucket"],
                Key = key
            }, cancellationToken);
            return File(response.ResponseStream, response.Headers.ContentType ?? "application/octet-stream", enableRangeProcessing: true);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
    }
}
