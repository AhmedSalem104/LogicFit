using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.Tenants.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Tenants.Commands.SetTenantStatus;

public class SetTenantStatusCommandHandler : IRequestHandler<SetTenantStatusCommand, PlatformTenantDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public SetTenantStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<PlatformTenantDto> Handle(SetTenantStatusCommand request, CancellationToken cancellationToken)
    {
        if (request.TenantId == PlatformConstants.PlatformTenantId)
        {
            throw new ForbiddenException("The platform tenant cannot be modified");
        }

        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId && !t.IsDeleted, cancellationToken);

        if (tenant == null)
        {
            throw new NotFoundException(nameof(Tenant), request.TenantId);
        }

        tenant.Status = request.Status;
        // Track why the gym is suspended (manual admin action) vs clear the reason once it's not suspended.
        tenant.SuspensionReason = request.Status == Domain.Enums.TenantStatus.Suspended
            ? Domain.Enums.SuspensionReason.ManualByAdmin
            : Domain.Enums.SuspensionReason.None;

        if (request.Status == TenantStatus.Active)
        {
            var now = _dateTimeService.UtcNow;
            var pendingOwnerMemberships = await _context.WorkspaceMemberships
                // This is a platform-scoped repair/approval action. It must not be hidden when
                // the request happens to carry a tenant context from a previous operation.
                .IgnoreQueryFilters()
                .Where(x => x.TenantId == tenant.Id
                    && x.Role == UserRole.Owner
                    && x.Status == WorkspaceMembershipStatus.PendingPlatformApproval
                    && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var membership in pendingOwnerMemberships)
            {
                membership.Status = WorkspaceMembershipStatus.Active;
                membership.ApprovedAt = now;
                membership.ApprovedBy = _currentUserService.UserId;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new PlatformTenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            WorkspaceType = tenant.WorkspaceType,
            Status = tenant.Status,
            Email = tenant.Email,
            PhoneNumber = tenant.PhoneNumber,
            CreatedAt = tenant.CreatedAt,
            IsDeleted = tenant.IsDeleted,
            DeletedAt = tenant.DeletedAt
        };
    }
}
