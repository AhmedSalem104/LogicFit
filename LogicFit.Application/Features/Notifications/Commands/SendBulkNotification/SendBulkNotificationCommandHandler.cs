using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Notifications.Commands.SendBulkNotification;

public class SendBulkNotificationCommandHandler : IRequestHandler<SendBulkNotificationCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public SendBulkNotificationCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(SendBulkNotificationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var senderId = Guid.Parse(_currentUserService.UserId!);

        var senderRole = await _context.Users
            .Where(u => u.Id == senderId && u.TenantId == tenantId)
            .Select(u => u.Role)
            .FirstOrDefaultAsync(cancellationToken);

        if (senderRole == UserRole.Client)
            throw new ForbiddenException("Clients cannot send notifications");

        var validRecipientIds = await _context.Users
            .Where(u => request.RecipientIds.Contains(u.Id) && u.TenantId == tenantId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var recipientId in validRecipientIds)
        {
            _context.Notifications.Add(new Notification
            {
                TenantId = tenantId,
                SenderId = senderId,
                RecipientId = recipientId,
                Title = request.Title,
                Body = request.Body,
                Type = request.Type,
                IsRead = false
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return validRecipientIds.Count;
    }
}
