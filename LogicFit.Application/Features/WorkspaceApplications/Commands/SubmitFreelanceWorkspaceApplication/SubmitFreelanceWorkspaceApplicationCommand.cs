using FluentValidation;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.SubmitFreelanceWorkspaceApplication;

/// <summary>Public request to create an independently branded FreelanceCoach workspace.</summary>
public sealed class SubmitFreelanceWorkspaceApplicationCommand : IRequest<ApplicationTrackingSessionDto>
{
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string Password { get; init; } = string.Empty;
    public string WorkspaceName { get; init; } = string.Empty;
    public string WorkspaceIdentifier { get; init; } = string.Empty;
    public string OwnerFullName { get; init; } = string.Empty;
    public string? BrandName { get; init; }
    public string? LogoUrl { get; init; }
    public string? PhotoUrl { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? BackgroundImageUrl { get; init; }
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public string? Bio { get; init; }
    public IReadOnlyList<string>? Specialties { get; init; }
    public IReadOnlyList<string>? Certifications { get; init; }
    public IReadOnlyDictionary<string, string>? SocialLinks { get; init; }
    public string? WelcomeMessage { get; init; }
    public System.Text.Json.JsonElement? BookingSettings { get; init; }
}

public sealed class SubmitFreelanceWorkspaceApplicationValidator : AbstractValidator<SubmitFreelanceWorkspaceApplicationCommand>
{
    public SubmitFreelanceWorkspaceApplicationValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.WorkspaceName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.WorkspaceIdentifier).NotEmpty().MaximumLength(100)
            .Matches("^[a-zA-Z0-9](?:[a-zA-Z0-9-]{1,98}[a-zA-Z0-9])?$");
        RuleFor(x => x.OwnerFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Bio).MaximumLength(4000);
        RuleFor(x => x.WelcomeMessage).MaximumLength(1000);
    }
}
