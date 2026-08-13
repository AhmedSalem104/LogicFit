using Microsoft.AspNetCore.Authorization;

namespace LogicFit.Infrastructure.Authorization;

public sealed class WorkspaceCapabilityRequirement(string capability) : IAuthorizationRequirement
{
    public string Capability { get; } = capability;
}
