using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Branches.Commands.UpdateBranch;

public class UpdateBranchCommandHandler : IRequestHandler<UpdateBranchCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateBranchCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Branch not found");

        if (request.IsDefault && !branch.IsDefault)
        {
            var others = await _context.Branches
                .Where(b => b.TenantId == tenantId && b.IsDefault && b.Id != branch.Id)
                .ToListAsync(cancellationToken);
            foreach (var b in others) b.IsDefault = false;
        }

        branch.Name = request.Name;
        branch.Code = request.Code;
        branch.Description = request.Description;
        branch.Address = request.Address;
        branch.City = request.City;
        branch.PhoneNumber = request.PhoneNumber;
        branch.Email = request.Email;
        branch.Latitude = request.Latitude;
        branch.Longitude = request.Longitude;
        branch.IsActive = request.IsActive;
        branch.IsDefault = request.IsDefault;
        branch.Capacity = request.Capacity;
        branch.OpenTime = request.OpenTime;
        branch.CloseTime = request.CloseTime;
        branch.ManagerId = request.ManagerId;
        branch.LogoUrl = request.LogoUrl;
        branch.CoverImageUrl = request.CoverImageUrl;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
