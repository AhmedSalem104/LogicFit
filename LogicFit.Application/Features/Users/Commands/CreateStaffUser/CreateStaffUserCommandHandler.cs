using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Users.Commands.CreateStaffUser;

public class CreateStaffUserCommandHandler : IRequestHandler<CreateStaffUserCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IRbacService _rbacService;

    // Only these back-office roles can be created here.
    private static readonly Dictionary<UserRole, string> AllowedRoles = new()
    {
        [UserRole.Manager] = SystemRoles.Manager,
        [UserRole.Receptionist] = SystemRoles.Receptionist,
        [UserRole.Accountant] = SystemRoles.Accountant,
        [UserRole.Trainer] = SystemRoles.Trainer
    };

    public CreateStaffUserCommandHandler(IApplicationDbContext context, ITenantService tenantService, IRbacService rbacService)
    {
        _context = context;
        _tenantService = tenantService;
        _rbacService = rbacService;
    }

    public async Task<Guid> Handle(CreateStaffUserCommand request, CancellationToken cancellationToken)
    {
        if (!AllowedRoles.TryGetValue(request.Role, out var roleName))
        {
            throw new ValidationException("Role", "Allowed staff roles are: Manager, Receptionist, Accountant, Trainer.");
        }

        var tenantId = _tenantService.GetCurrentTenantId();

        var phoneTaken = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.PhoneNumber == request.PhoneNumber, cancellationToken);
        if (phoneTaken != null)
        {
            throw new ConflictException("Phone number already registered");
        }

        var password = request.Password ?? $"{request.PhoneNumber}@{Guid.NewGuid().ToString("N")[..6]}";

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email ?? $"{request.PhoneNumber}@staff.logicfit.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = request.Role,
            IsActive = true
        };
        _context.Users.Add(user);

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            _context.UserProfiles.Add(new UserProfile { UserId = user.Id, FullName = request.FullName });
        }

        await _rbacService.EnsureUserInRoleAsync(user.Id, tenantId, roleName, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
