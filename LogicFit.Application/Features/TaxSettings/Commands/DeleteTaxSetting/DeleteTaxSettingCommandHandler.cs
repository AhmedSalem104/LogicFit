using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.TaxSettings.Commands.DeleteTaxSetting;

public class DeleteTaxSettingCommandHandler : IRequestHandler<DeleteTaxSettingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteTaxSettingCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(DeleteTaxSettingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var tax = await _context.TaxSettings
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("TaxSetting", request.Id);

        _context.TaxSettings.Remove(tax);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
