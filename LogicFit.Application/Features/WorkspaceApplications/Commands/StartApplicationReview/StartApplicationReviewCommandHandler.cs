using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.StartApplicationReview;

public sealed class StartApplicationReviewCommandHandler
    : IRequestHandler<StartApplicationReviewCommand, PlatformApplicationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public StartApplicationReviewCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<PlatformApplicationDto> Handle(StartApplicationReviewCommand request, CancellationToken cancellationToken)
    {
        var application = await _context.ApplicationRequests
            .Include(x => x.IdentityAccount)
            .FirstOrDefaultAsync(x => x.Id == request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApplicationRequest), request.ApplicationId);
        if (!ApplicationRequestStateMachine.CanTransition(application.Status, ApplicationRequestStatus.UnderReview))
            throw new ConflictException("This application cannot enter review.");

        _context.Entry(application).Property(nameof(ApplicationRequest.RowVersion)).OriginalValue = Convert.FromBase64String(request.RowVersion);
        application.Status = ApplicationRequestStatus.UnderReview;
        application.ReviewedAt = _dateTimeService.UtcNow;
        application.ReviewedBy = _currentUserService.UserId;
        await _context.SaveChangesAsync(cancellationToken);
        return PlatformApplicationMapper.ToDto(application, application.IdentityAccount.Email, application.IdentityAccount.PhoneNumber);
    }
}
