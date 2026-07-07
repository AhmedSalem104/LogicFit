using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Notifications;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public NotificationService(IApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task NotifyTenantOwnerAsync(
        Guid tenantId,
        string templateCode,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var owner = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Role == UserRole.Owner && !u.IsDeleted, cancellationToken);

        if (owner == null)
        {
            return; // Nothing to notify.
        }

        var rendered = NotificationTemplates.Render(templateCode, data);

        // In-app notification (persisted by the caller's SaveChanges). Sender == recipient for system messages.
        _context.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            SenderId = owner.Id,
            RecipientId = owner.Id,
            Title = rendered.Title,
            Body = rendered.Body,
            Type = rendered.Type,
            IsRead = false
        });

        // Email channel (best-effort; logging implementation by default).
        if (!string.IsNullOrWhiteSpace(owner.Email))
        {
            await _emailService.SendAsync(owner.Email, rendered.Title, rendered.Body, cancellationToken);
        }
    }
}
