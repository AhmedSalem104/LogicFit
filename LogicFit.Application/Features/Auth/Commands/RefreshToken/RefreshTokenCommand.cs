using System.Text.Json.Serialization;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommand : IRequest<AuthResponseDto>
{
    public string RefreshToken { get; set; } = string.Empty;

    // Set server-side by the controller (tenant vs platform API); never bound from the request body.
    [JsonIgnore]
    public string Surface { get; set; } = RefreshTokenService.SurfaceTenant;
}
