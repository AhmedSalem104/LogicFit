using FluentValidation;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.SponsorFreelanceMembership;

/// <summary>Freelance Owner proposes a team/client membership; Platform Admin remains the final approver.</summary>
public sealed class SponsorFreelanceMembershipCommand : IRequest<ApplicationTrackingStatusDto>
{
    public string IdentityEmail { get; init; } = string.Empty;
    public UserRole RequestedRole { get; init; }
    public string FullName { get; init; } = string.Empty;
}

public sealed class SponsorFreelanceMembershipValidator : AbstractValidator<SponsorFreelanceMembershipCommand>
{
    public SponsorFreelanceMembershipValidator()
    {
        RuleFor(x => x.IdentityEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RequestedRole).Must(x => x is UserRole.FreelanceCoach or UserRole.FreelanceAssistant or UserRole.Client)
            .WithMessage("RequestedRole must be FreelanceCoach, FreelanceAssistant, or Client.");
    }
}
