using System.Security.Cryptography;
using System.Text;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.WorkspaceMembers.DTOs;
using LogicFit.Application.Features.Identity;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceMembers.Commands.CreateWorkspaceMember;

public sealed class CreateWorkspaceMemberCommandHandler : IRequestHandler<CreateWorkspaceMemberCommand, WorkspaceMemberCreatedDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IRbacService _rbacService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _clock;

    public CreateWorkspaceMemberCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IRbacService rbacService,
        ICurrentUserService currentUser,
        IDateTimeService clock)
        => (_context, _tenantService, _rbacService, _currentUser, _clock) = (context, tenantService, rbacService, currentUser, clock);

    public async Task<WorkspaceMemberCreatedDto> Handle(CreateWorkspaceMemberCommand request, CancellationToken cancellationToken)
    {
        if (!WorkspaceMemberMapping.IsAllowedRole(request.Role))
            throw new ValidationException("Role", "Only Coach, Trainer, Manager, Receptionist, or Accountant can be created here.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("Email", "Email is required for identity login.");
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ValidationException("FullName", "Full name is required.");

        var tenantId = _tenantService.CurrentTenantId
            ?? throw new ForbiddenException("A workspace context is required.");

        var normalizedEmail = IdentityEmailAddress.Normalize(request.Email);
        var normalizedPhone = PhoneNumberNormalizer.NormalizeOptional(request.PhoneNumber);
        var identity = await _context.IdentityAccounts
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        var newIdentity = identity is null;
        string? temporaryPassword = null;

        if (identity is not null)
        {
            if (!identity.IsActive)
                throw new ConflictException("WORKSPACE_MEMBER_IDENTITY_INACTIVE", "This identity is inactive.");
            if (identity.EmailVerifiedAt is null)
                throw new ConflictException("WORKSPACE_MEMBER_IDENTITY_UNVERIFIED", "This identity must verify its email before it can be added.");
            if (normalizedPhone is not null && identity.NormalizedPhoneNumber is not null && identity.NormalizedPhoneNumber != normalizedPhone)
                throw new ConflictException("WORKSPACE_MEMBER_PHONE_MISMATCH", "The existing identity uses a different phone number.");
            if (normalizedPhone is not null && identity.NormalizedPhoneNumber is null)
            {
                var phoneUsedByAnotherIdentity = await _context.IdentityAccounts.IgnoreQueryFilters()
                    .AnyAsync(x => x.Id != identity.Id && x.NormalizedPhoneNumber == normalizedPhone && x.IsActive, cancellationToken);
                if (phoneUsedByAnotherIdentity)
                    throw new ConflictException("WORKSPACE_MEMBER_PHONE_DUPLICATE", "An identity already uses this phone number.");

                identity.PhoneNumber = request.PhoneNumber?.Trim();
                identity.NormalizedPhoneNumber = normalizedPhone;
            }
        }
        else
        {
            if (normalizedPhone is not null && await _context.IdentityAccounts.IgnoreQueryFilters()
                    .AnyAsync(x => x.NormalizedPhoneNumber == normalizedPhone && x.IsActive, cancellationToken))
                    throw new ConflictException("WORKSPACE_MEMBER_PHONE_DUPLICATE", "An identity already uses this phone number.");
            temporaryPassword = GenerateTemporaryPassword();
            identity = new IdentityAccount
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                PhoneNumber = request.PhoneNumber?.Trim(),
                NormalizedPhoneNumber = normalizedPhone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
                EmailVerifiedAt = _clock.UtcNow,
                IsActive = true
            };
            _context.IdentityAccounts.Add(identity);
        }

        var membership = await _context.WorkspaceMemberships
            .IgnoreQueryFilters()
            .Include(x => x.User)
                .ThenInclude(x => x.Profile)
            .Include(x => x.IdentityAccount)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IdentityAccountId == identity.Id, cancellationToken);
        if (membership is not null && !membership.IsDeleted && membership.Status != WorkspaceMembershipStatus.Revoked)
            throw new ConflictException("WORKSPACE_MEMBER_ALREADY_EXISTS", "This identity already belongs to the workspace.");

        var user = membership?.User ?? await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IdentityAccountId == identity.Id, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                TenantId = tenantId,
                IdentityAccountId = identity.Id,
                Email = identity.Email,
                PhoneNumber = request.PhoneNumber?.Trim() ?? identity.PhoneNumber,
                PasswordHash = identity.PasswordHash,
                Role = request.Role,
                IsActive = true,
                MustChangePassword = newIdentity
            };
            _context.Users.Add(user);
        }
        else
        {
            user.Email = identity.Email;
            user.PhoneNumber = request.PhoneNumber?.Trim() ?? identity.PhoneNumber;
            user.PasswordHash = identity.PasswordHash;
            user.Role = request.Role;
            user.IsActive = true;
            user.IsDeleted = false;
            user.DeletedAt = null;
            user.DeletedBy = null;
        }

        if (user.Profile is null)
        {
            var profile = new UserProfile { UserId = user.Id, FullName = request.FullName.Trim(), User = user };
            user.Profile = profile;
            _context.UserProfiles.Add(profile);
        }
        else
            user.Profile.FullName = request.FullName.Trim();

        var desiredRoleName = WorkspaceMemberMapping.RoleName(request.Role);
        var existingAssignments = await _context.UserRoleAssignments.IgnoreQueryFilters()
            .Where(x => x.UserId == user.Id && x.TenantId == tenantId)
            .Include(x => x.Role)
            .ToListAsync(cancellationToken);
        var hasDesiredRole = existingAssignments.Any(x => x.Role.Name == desiredRoleName);
        _context.UserRoleAssignments.RemoveRange(existingAssignments.Where(x => x.Role.Name != desiredRoleName));
        if (!hasDesiredRole)
            await _rbacService.EnsureUserInRoleAsync(user.Id, tenantId, desiredRoleName, cancellationToken);

        if (membership is null)
        {
            membership = new WorkspaceMembership
            {
                TenantId = tenantId,
                IdentityAccountId = identity.Id,
                UserId = user.Id,
                Role = request.Role,
                IdentityAccount = identity,
                User = user
            };
            _context.WorkspaceMemberships.Add(membership);
        }
        else
        {
            membership.IdentityAccount = identity;
            membership.User = user;
        }
        membership.IsDeleted = false;
        membership.DeletedAt = null;
        membership.DeletedBy = null;
        membership.Role = request.Role;
        membership.Status = WorkspaceMembershipStatus.Active;
        membership.ApprovedAt = _clock.UtcNow;
        membership.ApprovedBy = _currentUser.UserId;
        membership.RejectedAt = null;
        membership.RejectedBy = null;
        membership.DecisionReason = null;

        SecurityAuditLog.Add(_context, _currentUser, _clock, "WorkspaceMemberCreated", true, user.Id, tenantId);
        await _context.SaveChangesAsync(cancellationToken);

        return new WorkspaceMemberCreatedDto
        {
            Member = WorkspaceMemberMapping.ToDto(membership, _clock.UtcNow),
            NewIdentity = newIdentity,
            OneTimeCredentials = temporaryPassword is null ? null : new OneTimeWorkspaceMemberCredentialsDto
            {
                Email = identity.Email,
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
