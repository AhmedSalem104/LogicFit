using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Profile.Commands.DeleteProfilePicture;
using LogicFit.Application.Features.Profile.Commands.UpdateMyProfile;
using LogicFit.Application.Features.Profile.Commands.UpdateProfilePicture;
using LogicFit.Application.Features.Profile.Queries.GetMyProfile;
using LogicFit.Application.Features.Users.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Profile;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileUploadService _fileUploadService;

    public ProfileController(IMediator mediator, IFileUploadService fileUploadService)
    {
        _mediator = mediator;
        _fileUploadService = fileUploadService;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetMyProfile()
    {
        var result = await _mediator.Send(new GetMyProfileQuery());
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateMyProfile(UpdateMyProfileCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Upload profile picture
    /// </summary>
    [HttpPost("picture")]
    [ProducesResponseType(typeof(UploadProfilePictureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UploadProfilePictureResponse>> UploadProfilePicture(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        // Upload the file
        var url = await _fileUploadService.UploadImageAsync(file, "profile-pictures");

        // Update the profile with the new URL
        var result = await _mediator.Send(new UpdateProfilePictureCommand { ProfilePictureUrl = url });

        return Ok(new UploadProfilePictureResponse { Url = result });
    }

    /// <summary>
    /// Delete profile picture
    /// </summary>
    [HttpDelete("picture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteProfilePicture()
    {
        await _mediator.Send(new DeleteProfilePictureCommand());
        return NoContent();
    }
}

public class UploadProfilePictureResponse
{
    public string Url { get; set; } = string.Empty;
}
