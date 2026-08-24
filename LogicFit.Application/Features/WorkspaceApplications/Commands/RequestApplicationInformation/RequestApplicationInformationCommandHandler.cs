using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.RequestApplicationInformation;

public sealed class RequestApplicationInformationCommandHandler
    : IRequestHandler<RequestApplicationInformationCommand, PlatformApplicationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public RequestApplicationInformationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<PlatformApplicationDto> Handle(RequestApplicationInformationCommand request, CancellationToken cancellationToken)
    {
        var application = await _context.ApplicationRequests
            .Include(x => x.IdentityAccount)
            .FirstOrDefaultAsync(x => x.Id == request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApplicationRequest), request.ApplicationId);
        if (!ApplicationRequestStateMachine.CanTransition(application.Status, ApplicationRequestStatus.NeedsMoreInformation))
            throw new ConflictException("This application cannot be returned for more information.");

        var fields = request.RequestedFields.Distinct(StringComparer.Ordinal).ToArray();
        var fieldsAreValid = application.ApplicationType is ApplicationType.FreelanceWorkspaceCreation or ApplicationType.GymWorkspaceCreation
            ? FreelanceWorkspaceApplicationFields.AreAllowed(fields)
            : fields.All(x => x == "FullName");
        if (!fieldsAreValid)
            throw new ValidationException("RequestedFields", "One or more requested fields are not editable application fields.");

        _context.Entry(application).Property(nameof(ApplicationRequest.RowVersion)).OriginalValue = Convert.FromBase64String(request.RowVersion);
        var reviewVersion = Convert.ToHexString(application.RowVersion);
        application.Status = ApplicationRequestStatus.NeedsMoreInformation;
        application.InformationRequest = request.Message.Trim();
        application.RequestedFieldsJson = JsonSerializer.Serialize(fields);
        application.ReviewedAt = _dateTimeService.UtcNow;
        application.ReviewedBy = _currentUserService.UserId;
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Type = "workspace.application.information_requested",
            Payload = $"{{\"applicationId\":\"{application.Id}\"}}",
            OccurredAtUtc = _dateTimeService.UtcNow,
            IdempotencyKey = $"application:{application.Id}:information:{reviewVersion}"
        });
        await _context.SaveChangesAsync(cancellationToken);
        return PlatformApplicationMapper.ToDto(application, application.IdentityAccount.Email, application.IdentityAccount.PhoneNumber);
    }
}
