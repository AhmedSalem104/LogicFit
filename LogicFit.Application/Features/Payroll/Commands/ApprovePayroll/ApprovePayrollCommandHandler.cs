using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Payroll.Commands.ApprovePayroll;

public class ApprovePayrollCommandHandler : IRequestHandler<ApprovePayrollCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public ApprovePayrollCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(ApprovePayrollCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var run = await _context.PayrollRuns
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("PayrollRun", request.Id);

        if (run.Status != PayrollStatus.Draft)
            throw new DomainException("Only draft payroll runs can be approved");

        run.Status = PayrollStatus.Approved;
        run.ApprovedAt = _dateTimeService.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
