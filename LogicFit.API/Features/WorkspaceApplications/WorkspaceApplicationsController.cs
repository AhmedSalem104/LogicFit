using System.Security.Cryptography;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.Plans.DTOs;
using LogicFit.Application.Features.Platform.Plans.Queries.GetPlans;
using LogicFit.Application.Features.WorkspaceApplications.Commands.ResubmitApplication;
using LogicFit.Application.Features.WorkspaceApplications.Commands.SubmitFreelanceWorkspaceApplication;
using LogicFit.Application.Features.WorkspaceApplications.Commands.UpdateApplicationRequestedFields;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Application.Features.WorkspaceApplications.Queries.GetApplicationTrackingStatus;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.WorkspaceApplications;

/// <summary>
/// Public onboarding surface for independently operated FreelanceCoach workspaces. It deliberately
/// uses an opaque, short-lived tracking token instead of issuing normal tenant authentication.
/// </summary>
[ApiController]
[Route("api/workspace-applications")]
    [AllowAnonymous]
public sealed class WorkspaceApplicationsController : ControllerBase
{
    private const string TrackingTokenHeader = "X-Application-Tracking-Token";
    private readonly IMediator _mediator;
    private readonly IFileUploadService _fileUploadService;

    public WorkspaceApplicationsController(IMediator mediator, IFileUploadService fileUploadService)
    {
        _mediator = mediator;
        _fileUploadService = fileUploadService;
    }

    /// <summary>
    /// The short public onboarding flow. It accepts the workspace type, plan, basic owner/workspace
    /// data and payment proof in one request; identity, tenant, subscription and provisioning remain
    /// server-side lifecycle steps.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApplicationTrackingSessionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApplicationTrackingSessionDto>> Submit(
        [FromForm] SubmitWorkspaceApplicationForm form,
        [FromForm(Name = "proof")] IFormFile? proof,
        CancellationToken cancellationToken)
    {
        if (proof is null)
            throw new ValidationException("PaymentProof", "A payment proof file is required.");

        var proofUrl = await _fileUploadService.UploadDocumentAsync(proof, "payment-proofs");
        await using var proofStream = proof.OpenReadStream();
        var hash = await SHA256.HashDataAsync(proofStream, cancellationToken);

        var result = await _mediator.Send(new SubmitFreelanceWorkspaceApplicationCommand
        {
            WorkspaceType = form.WorkspaceType,
            Email = form.Email,
            PhoneNumber = form.PhoneNumber,
            Password = form.Password,
            WorkspaceName = form.WorkspaceName,
            WorkspaceIdentifier = form.WorkspaceIdentifier,
            OwnerFullName = form.OwnerFullName,
            BrandName = form.BrandName,
            Bio = form.Bio ?? form.Description,
            DeliveryMode = form.DeliveryMode,
            Specialties = string.IsNullOrWhiteSpace(form.Specialization)
                ? null
                : new[] { form.Specialization.Trim() },
            WelcomeMessage = form.WelcomeMessage,
            PlanId = form.PlanId,
            BillingCycle = form.BillingCycle,
            PaymentTransactionNumber = form.PaymentTransactionNumber,
            PaymentDate = form.PaymentDate,
            ProofStorageKey = proofUrl,
            ProofOriginalFileName = proof.FileName,
            ProofContentType = proof.ContentType ?? string.Empty,
            ProofSizeBytes = proof.Length,
            ProofSha256 = Convert.ToHexString(hash),
            IdempotencyKey = string.IsNullOrWhiteSpace(form.IdempotencyKey)
                ? $"public-{Guid.NewGuid():N}"
                : form.IdempotencyKey.Trim()
        }, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("plans")]
    [ProducesResponseType(typeof(List<PlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PlanDto>>> GetPlans(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPlansQuery { ActiveOnly = true }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("freelance")]
    [ProducesResponseType(typeof(ApplicationTrackingSessionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApplicationTrackingSessionDto>> SubmitFreelance(
        [FromBody] SubmitFreelanceWorkspaceApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("tracking")]
    [ProducesResponseType(typeof(ApplicationTrackingStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationTrackingStatusDto>> GetTrackingStatus(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetApplicationTrackingStatusQuery(GetTrackingToken()), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("tracking/fields")]
    [ProducesResponseType(typeof(ApplicationTrackingStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationTrackingStatusDto>> UpdateRequestedFields(
        [FromBody] IReadOnlyDictionary<string, System.Text.Json.JsonElement> fields,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateApplicationRequestedFieldsCommand
        {
            TrackingToken = GetTrackingToken(),
            Fields = fields
        }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("tracking/resubmit")]
    [ProducesResponseType(typeof(ApplicationTrackingStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationTrackingStatusDto>> Resubmit(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResubmitApplicationCommand(GetTrackingToken()), cancellationToken);
        return Ok(result);
    }

    private string GetTrackingToken() => Request.Headers[TrackingTokenHeader].ToString();
}

/// <summary>Only the user-facing onboarding fields are accepted from the public form.</summary>
public sealed class SubmitWorkspaceApplicationForm
{
    public WorkspaceType WorkspaceType { get; set; } = WorkspaceType.FreelanceCoach;
    public Guid PlanId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
    public string WorkspaceIdentifier { get; set; } = string.Empty;
    public string OwnerFullName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string? Specialization { get; set; }
    public string? DeliveryMode { get; set; }
    public string? Description { get; set; }
    public string? Bio { get; set; }
    public string? WelcomeMessage { get; set; }
    public BillingCycle? BillingCycle { get; set; }
    public string? PaymentTransactionNumber { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? IdempotencyKey { get; set; }
}
