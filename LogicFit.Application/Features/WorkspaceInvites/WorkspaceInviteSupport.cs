using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;

namespace LogicFit.Application.Features.WorkspaceInvites;

internal static class WorkspaceInviteSupport
{
    public static string SystemRoleFor(UserRole role) => role switch
    {
        UserRole.FreelanceCoach => SystemRoles.FreelanceCoach,
        UserRole.FreelanceAssistant => SystemRoles.FreelanceAssistant,
        UserRole.Client => SystemRoles.Client,
        _ => throw new ConflictException("Unsupported workspace invitation role.")
    };

    public static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var local = email[..at];
        var visible = local.Length == 1 ? local : local[..Math.Min(2, local.Length)];
        return $"{visible}***{email[at..]}";
    }
}
