using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Interfaces;

public interface IWorkspaceMembershipQuotaService
{
    Task EnsureCapacityAsync(Guid workspaceId, UserRole requestedRole, CancellationToken cancellationToken = default);
}
