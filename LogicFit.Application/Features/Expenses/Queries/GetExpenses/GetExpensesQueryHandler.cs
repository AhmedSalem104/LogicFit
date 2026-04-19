using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Expenses.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Expenses.Queries.GetExpenses;

public class GetExpensesQueryHandler : IRequestHandler<GetExpensesQuery, List<ExpenseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetExpensesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ExpenseDto>> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Expenses
            .Include(e => e.Branch)
            .Include(e => e.Category)
            .Include(e => e.ApprovedBy)
            .Where(e => e.TenantId == tenantId)
            .AsQueryable();

        if (request.BranchId.HasValue)
            query = query.Where(e => e.BranchId == request.BranchId.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(e => e.CategoryId == request.CategoryId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(e => e.ExpenseDate <= request.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(e => e.Description.Contains(term)
                || (e.VendorName != null && e.VendorName.Contains(term)));
        }

        var expenses = await query.OrderByDescending(e => e.ExpenseDate).ToListAsync(cancellationToken);

        return expenses.Select(e => new ExpenseDto
        {
            Id = e.Id,
            TenantId = e.TenantId,
            BranchId = e.BranchId,
            BranchName = e.Branch?.Name,
            CategoryId = e.CategoryId,
            CategoryName = e.Category.Name,
            Amount = e.Amount,
            ExpenseDate = e.ExpenseDate,
            Description = e.Description,
            VendorName = e.VendorName,
            PaymentMethod = e.PaymentMethod,
            ReceiptImageUrl = e.ReceiptImageUrl,
            ReferenceNumber = e.ReferenceNumber,
            ApprovedById = e.ApprovedById,
            ApprovedByName = e.ApprovedBy?.Email,
            ApprovedAt = e.ApprovedAt
        }).ToList();
    }
}
