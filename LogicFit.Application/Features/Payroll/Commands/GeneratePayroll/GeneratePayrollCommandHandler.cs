using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Payroll.Commands.GeneratePayroll;

public class GeneratePayrollCommandHandler : IRequestHandler<GeneratePayrollCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GeneratePayrollCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(GeneratePayrollCommand request, CancellationToken cancellationToken)
    {
        if (request.Month < 1 || request.Month > 12)
            throw new DomainException("Invalid month");
        if (request.Year < 2000 || request.Year > 2100)
            throw new DomainException("Invalid year");

        var tenantId = _tenantService.GetCurrentTenantId();

        var existing = await _context.PayrollRuns
            .AnyAsync(r => r.TenantId == tenantId
                && r.Year == request.Year
                && r.Month == request.Month
                && r.BranchId == request.BranchId
                && r.Status != PayrollStatus.Cancelled, cancellationToken);
        if (existing)
            throw new ConflictException($"A payroll run for {request.Year}-{request.Month:D2} already exists for this branch");

        var employeesQuery = _context.EmployeeProfiles
            .Include(e => e.Branches)
            .Where(e => e.TenantId == tenantId && e.TerminationDate == null);

        if (request.BranchId.HasValue)
            employeesQuery = employeesQuery.Where(e => e.Branches.Any(b => b.BranchId == request.BranchId.Value));

        var employees = await employeesQuery.ToListAsync(cancellationToken);

        var periodStart = new DateTime(request.Year, request.Month, 1);
        var periodEnd = periodStart.AddMonths(1);

        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = request.BranchId,
            Month = request.Month,
            Year = request.Year,
            Status = PayrollStatus.Draft
        };
        _context.PayrollRuns.Add(run);

        decimal runTotal = 0;

        foreach (var employee in employees)
        {
            // Pick pending commissions earned within the period or before
            var commissions = await _context.Commissions
                .Where(c => c.TenantId == tenantId
                    && c.EmployeeId == employee.Id
                    && c.Status == CommissionStatus.Pending
                    && c.EarnedDate < periodEnd)
                .ToListAsync(cancellationToken);

            var commissionTotal = commissions.Sum(c => c.Amount);

            var item = new PayrollItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PayrollRunId = run.Id,
                EmployeeId = employee.Id,
                BaseSalary = employee.BaseSalary,
                CommissionTotal = commissionTotal,
                Bonus = 0,
                Deductions = 0,
                NetSalary = employee.BaseSalary + commissionTotal
            };
            _context.PayrollItems.Add(item);
            runTotal += item.NetSalary;

            foreach (var c in commissions)
            {
                c.PayrollItemId = item.Id;
                c.Status = CommissionStatus.Approved;
            }
        }

        run.TotalAmount = runTotal;
        await _context.SaveChangesAsync(cancellationToken);
        return run.Id;
    }
}
