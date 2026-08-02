using FluentValidation;
using LogicFit.Domain.Enums;
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
    public Guid PlanId { get; init; }
    public BillingCycle BillingCycle { get; init; } = BillingCycle.Monthly;
    public decimal PaymentAmount { get; init; }
    public string? PaymentTransactionNumber { get; init; }
    public DateTime? PaymentDate { get; init; }
    /// <summary>Opaque key returned by the private storage service; public URLs are not accepted.</summary>
    public string ProofStorageKey { get; init; } = string.Empty;
    public string ProofOriginalFileName { get; init; } = string.Empty;
    public string ProofContentType { get; init; } = string.Empty;
    public long ProofSizeBytes { get; init; }
    public string ProofSha256 { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
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
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.BillingCycle).IsInEnum();
        RuleFor(x => x.PaymentAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ProofStorageKey).NotEmpty().MaximumLength(500)
            .Must(value => !value.Contains("..", StringComparison.Ordinal) &&
                           !value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                           !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            .WithMessage("A private storage key is required; public URLs are not accepted.");
        RuleFor(x => x.ProofOriginalFileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ProofContentType).Must(value => value is "image/jpeg" or "image/png" or "application/pdf")
            .WithMessage("Payment proof must be a JPEG, PNG, or PDF.");
        RuleFor(x => x.ProofSizeBytes).InclusiveBetween(1, 10 * 1024 * 1024);
        RuleFor(x => x.ProofSha256).Matches("^[A-Fa-f0-9]{64}$");
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(100);
    }
}
