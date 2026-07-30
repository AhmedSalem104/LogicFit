using FluentValidation;
using LogicFit.Application.Features.WorkspaceInvites.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceInvites.Commands.CreateWorkspaceInvite;

public sealed record CreateWorkspaceInviteCommand(string Email, UserRole RequestedRole) : IRequest<WorkspaceInviteCreatedDto>;

public sealed class CreateWorkspaceInviteValidator : AbstractValidator<CreateWorkspaceInviteCommand>
{
    public CreateWorkspaceInviteValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.RequestedRole)
            .Must(x => x is UserRole.FreelanceCoach or UserRole.FreelanceAssistant)
            .WithMessage("Only FreelanceCoach and FreelanceAssistant can be invited through this endpoint.");
    }
}
