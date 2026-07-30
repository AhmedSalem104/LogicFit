using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.WorkspaceInvites.DTOs;

public sealed class WorkspaceInviteCreatedDto
{
    public Guid InviteId { get; init; }
    public string EmailMasked { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public DateTime ExpiresAt { get; init; }
}
