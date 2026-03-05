using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Notifications.Commands.SendNotification;

public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public SendNotificationCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var senderId = Guid.Parse(_currentUserService.UserId!);

        var recipientExists = await _context.Users
            .AnyAsync(u => u.Id == request.RecipientId && u.TenantId == tenantId, cancellationToken);

        if (!recipientExists)
            throw new NotFoundException("User", request.RecipientId);

        var notification = new Notification
        {
            TenantId = tenantId,
            SenderId = senderId,
            RecipientId = request.RecipientId,
            Title = request.Title,
            Body = request.Body,
            Type = request.Type,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return notification.Id;
    }
}
