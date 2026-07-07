using MediatR;

namespace LogicFit.Application.Features.Auth.Commands.LogoutAll;

/// <summary>Revokes every active refresh token for the current user (logout everywhere).</summary>
public class LogoutAllCommand : IRequest<Unit>
{
}
