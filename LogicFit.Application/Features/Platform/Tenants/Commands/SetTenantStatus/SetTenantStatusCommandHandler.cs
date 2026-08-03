using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.Tenants.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Tenants.Commands.SetTenantStatus;

public class SetTenantStatusCommandHandler : IRequestHandler<SetTenantStatusCommand, PlatformTenantDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;

    public SetTenantStatusCommandHandler(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _currentUserService = currentUserService;
    }

    public async Task<PlatformTenantDto> Handle(SetTenantStatusCommand request, CancellationToken cancellationToken)
    {
        if (request.TenantId == PlatformConstants.PlatformTenantId)
        {
            throw new ForbiddenException("The platform tenant cannot be modified");
        }

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            throw new NotFoundException(nameof(Tenant), request.TenantId);
        }

        tenant.Status = request.Status;
        // Track why the gym is suspended (manual admin action) vs clear the reason once it's not suspended.
        tenant.SuspensionReason = request.Status == Domain.Enums.TenantStatus.Suspended
            ? Domain.Enums.SuspensionReason.ManualByAdmin
            : Domain.Enums.SuspensionReason.None;

        if (request.Status == Domain.Enums.TenantStatus.Active)
        {
            var now = _dateTimeService.UtcNow;
            var approvedBy = _currentUserService.UserId ?? "platform-admin";
            var pendingMemberships = await _context.WorkspaceMemberships
                .IgnoreQueryFilters()
                .Where(x => x.TenantId == tenant.Id &&
                            x.Status == Domain.Enums.WorkspaceMembershipStatus.PendingPlatformApproval &&
                            !x.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var membership in pendingMemberships)
            {
                membership.Status = Domain.Enums.WorkspaceMembershipStatus.Active;
                membership.ApprovedAt = now;
                membership.ApprovedBy = approvedBy;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new PlatformTenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            Status = tenant.Status,
            Email = tenant.Email,
            PhoneNumber = tenant.PhoneNumber,
            CreatedAt = tenant.CreatedAt
        };
    }
}
