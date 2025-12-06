using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.GymProfile.Commands.UpdateGymProfile;
using LogicFit.Application.Features.GymProfile.DTOs;
using LogicFit.Application.Features.GymProfile.Queries.GetGymProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.GymProfile;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GymProfileController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileUploadService _fileUploadService;

    public GymProfileController(IMediator mediator, IFileUploadService fileUploadService)
    {
        _mediator = mediator;
        _fileUploadService = fileUploadService;
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
}

public class UploadResponseDto
{
    public string Url { get; set; } = string.Empty;
}

public class UploadMultipleResponseDto
{
    public List<string> Urls { get; set; } = new();
}
