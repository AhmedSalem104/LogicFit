using LogicFit.Application.Features.Identity.DTOs;

namespace LogicFit.Application.Common.Interfaces;

public interface IIdentityWorkspaceSessionIssuer
{
    Task<IdentitySignInDto> IssueAsync(Guid identityAccountId, CancellationToken cancellationToken = default);
}
