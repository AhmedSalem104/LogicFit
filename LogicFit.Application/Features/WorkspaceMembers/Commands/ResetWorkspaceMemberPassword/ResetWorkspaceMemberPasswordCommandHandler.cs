using System.Security.Cryptography;
using System.Text;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.WorkspaceMembers.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceMembers.Commands.ResetWorkspaceMemberPassword;

public sealed class ResetWorkspaceMemberPasswordCommandHandler : IRequestHandler<ResetWorkspaceMemberPasswordCommand, WorkspaceMemberCreatedDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _clock;

    public ResetWorkspaceMemberPasswordCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUser, IDateTimeService clock)
        => (_context, _tenantService, _currentUser, _clock) = (context, tenantService, currentUser, clock);

    public async Task<WorkspaceMemberCreatedDto> Handle(ResetWorkspaceMemberPasswordCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.CurrentTenantId ?? throw new ForbiddenException("A workspace context is required.");
        var membership = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            .Include(x => x.User).ThenInclude(x => x.Profile)
            .Include(x => x.IdentityAccount)
            .FirstOrDefaultAsync(x => x.Id == request.MembershipId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("WorkspaceMembership", request.MembershipId);
        if (membership.Status == WorkspaceMembershipStatus.Revoked)
            throw new ConflictException("WORKSPACE_MEMBER_REMOVED", "A removed member must be activated before resetting its password.");

        var temporaryPassword = GenerateTemporaryPassword();
        var hash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
        membership.IdentityAccount.PasswordHash = hash;
        membership.IdentityAccount.FailedLoginAttempts = 0;
        membership.IdentityAccount.LockoutEndUtc = null;
        membership.User.PasswordHash = hash;
        membership.User.MustChangePassword = true;
        SecurityAuditLog.Add(_context, _currentUser, _clock, "WorkspaceMemberPasswordReset", true, membership.UserId, tenantId);
        await _context.SaveChangesAsync(cancellationToken);

        return new WorkspaceMemberCreatedDto
        {
            Member = WorkspaceMemberMapping.ToDto(membership, _clock.UtcNow),
            NewIdentity = false,
            OneTimeCredentials = new OneTimeWorkspaceMemberCredentialsDto
            {
                Email = membership.IdentityAccount.Email,
                TemporaryPassword = temporaryPassword,
                MustChangePassword = true
            }
        };
    }

    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789-_";
        var bytes = RandomNumberGenerator.GetBytes(18);
        var builder = new StringBuilder(bytes.Length);
        foreach (var value in bytes)
            builder.Append(alphabet[value % alphabet.Length]);
        return builder.ToString();
    }
}
