using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.GymProfile.Commands.UpdateGymProfile;
using LogicFit.Application.Features.GymProfile.DTOs;
using LogicFit.Application.Features.GymProfile.Queries.GetGymProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.GymProfile;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageSettings)]
[Authorize(Policy = WorkspaceCapabilities.GymSettings)]
public class GymProfileController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileUploadService _fileUploadService;
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GymProfileController(IMediator mediator, IFileUploadService fileUploadService, IApplicationDbContext context, ITenantService tenantService)
    {
        _mediator = mediator;
        _fileUploadService = fileUploadService;
        _context = context;
        _tenantService = tenantService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GymProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GymProfileDto>> GetProfile()
    {
        var result = await _mediator.Send(new GetGymProfileQuery());
        return Ok(result);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateProfile([FromBody] UpdateGymProfileCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("logo")]
    [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadResponseDto>> UploadLogo(IFormFile file)
    {
        var url = await _fileUploadService.UploadImageAsync(file, "gym-logos");
        return Ok(new UploadResponseDto { Url = url });
    }

    [HttpPost("cover")]
    [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadResponseDto>> UploadCover(IFormFile file)
    {
        var url = await _fileUploadService.UploadImageAsync(file, "gym-covers");
        return Ok(new UploadResponseDto { Url = url });
    }

    [HttpPost("gallery")]
    [ProducesResponseType(typeof(UploadMultipleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadMultipleResponseDto>> UploadGalleryImages([FromForm] List<IFormFile> files)
    {
        var urls = await _fileUploadService.UploadImagesAsync(files, "gym-gallery");
        return Ok(new UploadMultipleResponseDto { Urls = urls });
    }

    [HttpPost("assets")]
    public async Task<ActionResult<BrandAssetResponse>> UploadBrandAsset([FromForm] IFormFile file, [FromForm] string assetType = "Gallery", [FromForm] string? title = null, [FromForm] string? altText = null)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var type = string.IsNullOrWhiteSpace(assetType) ? "Gallery" : assetType.Trim();
        if (string.Equals(type, "Gallery", StringComparison.OrdinalIgnoreCase) &&
            await _context.TenantBrandAssets.CountAsync(a => a.TenantId == tenantId && a.AssetType == "Gallery" && a.IsActive) >= 5)
            return BadRequest("A gym can have at most five active gallery branding assets.");

        var url = await _fileUploadService.UploadImageAsync(file, "tenant-brand-assets");
        var asset = new TenantBrandAsset
        {
            TenantId = tenantId, AssetType = type, ImageUrl = url,
            Title = title, AltText = altText,
            SortOrder = (await _context.TenantBrandAssets.Where(a => a.TenantId == tenantId && a.AssetType == type).Select(a => (int?)a.SortOrder).MaxAsync() ?? -1) + 1
        };
        _context.TenantBrandAssets.Add(asset);
        await _context.SaveChangesAsync();
        return Ok(new BrandAssetResponse { Id = asset.Id, ImageUrl = asset.ImageUrl, AssetType = asset.AssetType, SortOrder = asset.SortOrder });
    }

    [HttpDelete("assets/{id:guid}")]
    public async Task<IActionResult> DeleteBrandAsset(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var asset = await _context.TenantBrandAssets.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId);
        if (asset == null) return NotFound();
        _context.TenantBrandAssets.Remove(asset);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class UploadResponseDto
{
    public string Url { get; set; } = string.Empty;
}

public class UploadMultipleResponseDto
{
    public List<string> Urls { get; set; } = new();
}

public class BrandAssetResponse
{
    public Guid Id { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
