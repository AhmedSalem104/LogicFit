using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.GroupClasses.Commands.CreateGroupClass;

public class CreateGroupClassCommandHandler : IRequestHandler<CreateGroupClassCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public CreateGroupClassCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateGroupClassCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = Guid.Parse(_currentUserService.UserId!);
        var role = await _context.Users.Where(u => u.Id == currentUserId).Select(u => u.Role).FirstOrDefaultAsync(cancellationToken);
        if (role == UserRole.Client)
            throw new ForbiddenException("Clients cannot create group classes");

        var groupClass = new GroupClass
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.GetCurrentTenantId(),
            BranchId = request.BranchId,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            DurationMinutes = request.DurationMinutes,
            Capacity = request.Capacity,
            Color = request.Color,
            ImageUrl = request.ImageUrl,
            Price = request.Price,
            IsActive = request.IsActive
        };

        _context.GroupClasses.Add(groupClass);
        await _context.SaveChangesAsync(cancellationToken);
        return groupClass.Id;
    }
}
