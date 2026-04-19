using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Payroll.Commands.PayPayroll;

public class PayPayrollCommandHandler : IRequestHandler<PayPayrollCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public PayPayrollCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(PayPayrollCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;

        var run = await _context.PayrollRuns
            .Include(r => r.Items)
                .ThenInclude(i => i.Commissions)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("PayrollRun", request.Id);

        if (run.Status != PayrollStatus.Approved)
            throw new DomainException("Only approved payroll runs can be paid");

        run.Status = PayrollStatus.Paid;
        run.PaidAt = now;

        foreach (var item in run.Items)
        {
            item.PaidAt = now;
            foreach (var c in item.Commissions)
                c.Status = CommissionStatus.Paid;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
