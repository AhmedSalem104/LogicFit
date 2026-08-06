using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Identity;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Coaches.Commands.CreateCoach;

public class CreateCoachCommandHandler : IRequestHandler<CreateCoachCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IRbacService _rbacService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public CreateCoachCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IRbacService rbacService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _rbacService = rbacService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Guid> Handle(CreateCoachCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // A coach is a global identity plus a tenant-local user/membership. Keeping only the
        // legacy User row makes the account invisible to /api/identity/login after logout.
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.PhoneNumber == request.PhoneNumber, cancellationToken);

        if (existingUser != null)
            throw new ConflictException("Phone number already registered");

        var email = request.Email!.Trim();
        var normalizedEmail = IdentityEmailAddress.Normalize(email);
        var existingEmailUser = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email.ToUpper() == normalizedEmail, cancellationToken);
        if (existingEmailUser != null)
            throw new ConflictException("Email already registered in this workspace");

        var identity = await _context.IdentityAccounts
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (identity is not null)
        {
            if (!identity.IsActive)
                throw new ConflictException("The coach identity is inactive");
            if (identity.EmailVerifiedAt is null)
                throw new ConflictException("The coach email must be verified before adding this identity");

            var existingMembership = await _context.WorkspaceMemberships
                .IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.IdentityAccountId == identity.Id && !x.IsDeleted, cancellationToken);
            if (existingMembership)
                throw new ConflictException("This identity already belongs to the workspace");
        }
        else
        {
            // This is an explicit gym-owner-provisioned account, analogous to platform-provisioned
            // owners: the supplied password is already known to the recipient and the email is
            // recorded as verified so the user can enter the workspace immediately.
            identity = new IdentityAccount
            {
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? email : request.FullName.Trim(),
                Email = email,
                NormalizedEmail = normalizedEmail,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password!),
                EmailVerifiedAt = _dateTimeService.UtcNow,
                IsActive = true
            };
            _context.IdentityAccounts.Add(identity);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            IdentityAccountId = identity.Id,
            PhoneNumber = request.PhoneNumber,
            Email = identity.Email,
            PasswordHash = identity.PasswordHash,
            Role = UserRole.Coach,
            IsActive = true,
            WalletBalance = 0,
            StaffQrCode = $"staff:{tenantId:N}:{Guid.NewGuid():N}",
            StaffQrGeneratedAt = _dateTimeService.UtcNow
        };

        _context.Users.Add(user);

        // Create profile if any profile data provided
        if (!string.IsNullOrEmpty(request.FullName) || request.Gender.HasValue || request.BirthDate.HasValue)
        {
            var profile = new UserProfile
            {
                UserId = user.Id,
                FullName = request.FullName,
                Gender = request.Gender.HasValue ? (GenderType)request.Gender.Value : null,
                BirthDate = request.BirthDate
            };
            _context.UserProfiles.Add(profile);
        }

        // Assign the Coach RBAC role so the coach's permissions resolve at login.
        await _rbacService.EnsureUserInRoleAsync(user.Id, tenantId, SystemRoles.Coach, cancellationToken);

        _context.WorkspaceMemberships.Add(new WorkspaceMembership
        {
            TenantId = tenantId,
            IdentityAccountId = identity.Id,
            UserId = user.Id,
            Role = UserRole.Coach,
            Status = WorkspaceMembershipStatus.Active,
            ApprovedAt = _dateTimeService.UtcNow,
            ApprovedBy = _currentUserService.UserId
        });

        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
