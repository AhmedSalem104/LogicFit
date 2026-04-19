using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.TaxSettings.Commands.UpdateTaxSetting;

public class UpdateTaxSettingCommandHandler : IRequestHandler<UpdateTaxSettingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateTaxSettingCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(UpdateTaxSettingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var tax = await _context.TaxSettings
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("TaxSetting", request.Id);

        if (request.IsDefault && !tax.IsDefault)
        {
            var existing = await _context.TaxSettings.Where(t => t.TenantId == tenantId && t.IsDefault && t.Id != tax.Id).ToListAsync(cancellationToken);
            foreach (var t in existing) t.IsDefault = false;
        }

        tax.Name = request.Name;
        tax.Rate = request.Rate;
        tax.IsDefault = request.IsDefault;
        tax.IsActive = request.IsActive;
        tax.Description = request.Description;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
