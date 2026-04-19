using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Leaves.Commands.ReviewLeaveRequest;

public class ReviewLeaveRequestCommandHandler : IRequestHandler<ReviewLeaveRequestCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public ReviewLeaveRequestCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(ReviewLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        if (request.Decision != LeaveStatus.Approved && request.Decision != LeaveStatus.Rejected)
            throw new DomainException("Decision must be Approved or Rejected");

        var tenantId = _tenantService.GetCurrentTenantId();
        var leave = await _context.LeaveRequests
            .FirstOrDefaultAsync(l => l.Id == request.Id && l.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("LeaveRequest", request.Id);

        if (leave.Status != LeaveStatus.Pending)
            throw new DomainException("Only pending leaves can be reviewed");

        leave.Status = request.Decision;
        leave.ReviewNotes = request.Notes;
        leave.ReviewedAt = _dateTimeService.UtcNow;
        if (Guid.TryParse(_currentUserService.UserId, out var uid))
            leave.ReviewedById = uid;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
