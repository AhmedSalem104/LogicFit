using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Branches.Commands.CreateBranch;

public class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateBranchCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        if (request.IsDefault)
        {
            var existingDefault = await _context.Branches
                .Where(b => b.TenantId == tenantId && b.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var b in existingDefault)
                b.IsDefault = false;
        }

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Address = request.Address,
            City = request.City,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = request.IsActive,
            IsDefault = request.IsDefault,
            Capacity = request.Capacity,
            OpenTime = request.OpenTime,
            CloseTime = request.CloseTime,
            ManagerId = request.ManagerId,
            LogoUrl = request.LogoUrl,
            CoverImageUrl = request.CoverImageUrl
        };

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync(cancellationToken);

        return branch.Id;
    }
}
