using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using MediatR;

namespace LogicFit.Application.Features.GroupClasses.Commands.CreateGroupClass;

public class CreateGroupClassCommandHandler : IRequestHandler<CreateGroupClassCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateGroupClassCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateGroupClassCommand request, CancellationToken cancellationToken)
    {
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
