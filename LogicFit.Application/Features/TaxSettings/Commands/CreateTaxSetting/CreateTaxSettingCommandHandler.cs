using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.TaxSettings.Commands.CreateTaxSetting;

public class CreateTaxSettingCommandHandler : IRequestHandler<CreateTaxSettingCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateTaxSettingCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateTaxSettingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        if (request.IsDefault)
        {
            var existing = await _context.TaxSettings.Where(t => t.TenantId == tenantId && t.IsDefault).ToListAsync(cancellationToken);
            foreach (var t in existing) t.IsDefault = false;
        }

        var tax = new TaxSetting
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Rate = request.Rate,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            Description = request.Description
        };

        _context.TaxSettings.Add(tax);
        await _context.SaveChangesAsync(cancellationToken);
        return tax.Id;
    }
}
