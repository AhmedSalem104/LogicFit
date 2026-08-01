using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Services;

/// <summary>
/// Creates deliberately minimal authentication audit records. Never pass credentials,
/// tokens, phone numbers, email addresses, or provider secrets to this helper.
/// </summary>
public static class SecurityAuditLog
{
    public static void Add(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeService clock,
        string eventName,
        bool success,
        Guid? subjectId = null,
        Guid? tenantId = null)
    {
        context.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            UserId = currentUser.UserId,
            Action = AuditAction.Create,
            EntityName = "SecurityAuthEvent",
            EntityId = subjectId?.ToString() ?? "anonymous",
            NewValues = JsonSerializer.Serialize(new { Event = eventName, Success = success }),
            Timestamp = clock.UtcNow,
            IpAddress = currentUser.IpAddress,
            UserAgent = currentUser.UserAgent
        });
    }
}
