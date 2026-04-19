using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Payroll.Commands.UpdatePayrollItem;

public class UpdatePayrollItemCommandHandler : IRequestHandler<UpdatePayrollItemCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdatePayrollItemCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(UpdatePayrollItemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var item = await _context.PayrollItems
            .Include(p => p.PayrollRun)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("PayrollItem", request.Id);

        if (item.PayrollRun.Status != PayrollStatus.Draft)
            throw new DomainException("Only draft payroll runs can be modified");

        if (request.Bonus.HasValue) item.Bonus = request.Bonus.Value;
        if (request.Deductions.HasValue) item.Deductions = request.Deductions.Value;
        if (request.Notes != null) item.Notes = request.Notes;

        item.NetSalary = item.BaseSalary + item.CommissionTotal + item.Bonus - item.Deductions;

        // Recalculate run total
        var allItems = await _context.PayrollItems
            .Where(p => p.PayrollRunId == item.PayrollRunId && p.Id != item.Id && p.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        item.PayrollRun.TotalAmount = allItems.Sum(p => p.NetSalary) + item.NetSalary;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
