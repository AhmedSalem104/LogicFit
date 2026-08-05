using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.RetryWorkspaceProvisioning;

/// <summary>
/// Resumes a failed or capacity-blocked provisioning job without creating a new application,
/// subscription, tenant, or membership. The saga owns idempotency and cross-database retries.
/// </summary>
public sealed class RetryWorkspaceProvisioningCommandHandler(
    IApplicationDbContext context,
    IWorkspaceProvisioningSaga provisioningSaga)
    : IRequestHandler<RetryWorkspaceProvisioningCommand, PlatformApplicationDto>
{
    public async Task<PlatformApplicationDto> Handle(
        RetryWorkspaceProvisioningCommand request,
        CancellationToken cancellationToken)
    {
        var application = await context.ApplicationRequests
            .Include(x => x.IdentityAccount)
            .FirstOrDefaultAsync(x => x.Id == request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApplicationRequest), request.ApplicationId);

        if (application.Status != ApplicationRequestStatus.Approved || !application.ProvisionedWorkspaceId.HasValue)
            throw new ConflictException("Only an approved workspace with a provisioning placeholder can be retried.");

        var outcome = await provisioningSaga.RunAsync(application.Id, cancellationToken);
        ProvisioningOutcomeGuard.EnsureCompleted(outcome);
        return PlatformApplicationMapper.ToDto(application, application.IdentityAccount.Email, application.IdentityAccount.PhoneNumber);
    }
}
