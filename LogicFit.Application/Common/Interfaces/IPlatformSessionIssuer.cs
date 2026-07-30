using LogicFit.Application.Features.Auth.DTOs;

namespace LogicFit.Application.Common.Interfaces;

public interface IPlatformSessionIssuer
{
    Task<AuthResponseDto> IssueAsync(Guid identityAccountId, CancellationToken cancellationToken = default);
}
