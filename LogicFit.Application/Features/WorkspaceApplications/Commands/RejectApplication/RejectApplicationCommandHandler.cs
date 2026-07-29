using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.RejectApplication;

public sealed class RejectApplicationCommandHandler : IRequestHandler<RejectApplicationCommand, PlatformApplicationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public RejectApplicationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<PlatformApplicationDto> Handle(RejectApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _context.ApplicationRequests
            .Include(x => x.IdentityAccount)
            .FirstOrDefaultAsync(x => x.Id == request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApplicationRequest), request.ApplicationId);
        if (!ApplicationRequestStateMachine.CanTransition(application.Status, ApplicationRequestStatus.Rejected))
            throw new ConflictException("This application cannot be rejected.");

        _context.Entry(application).Property(nameof(ApplicationRequest.RowVersion)).OriginalValue = Convert.FromBase64String(request.RowVersion);
        var now = _dateTimeService.UtcNow;
        application.Status = ApplicationRequestStatus.Rejected;
        application.DecisionReason = request.Reason.Trim();
        application.ReviewedAt = now;
        application.ReviewedBy = _currentUserService.UserId;
        var sessions = await _context.ApplicationTrackingSessions
            .Where(x => x.ApplicationRequestId == application.Id && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions) session.RevokedAt = now;
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Type = "workspace.application.rejected",
            Payload = $"{{\"applicationId\":\"{application.Id}\"}}",
            OccurredAtUtc = now,
            IdempotencyKey = $"application:{application.Id}:rejected"
        });
        await _context.SaveChangesAsync(cancellationToken);
        return PlatformApplicationMapper.ToDto(application, application.IdentityAccount.Email, application.IdentityAccount.PhoneNumber);
    }
}
