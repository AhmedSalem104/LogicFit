using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Suppliers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Suppliers.Queries.GetSuppliers;

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, List<SupplierDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetSuppliersQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Suppliers.Where(s => s.TenantId == tenantId).AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(s => s.Name.Contains(term)
                || (s.ContactPerson != null && s.ContactPerson.Contains(term))
                || (s.Phone != null && s.Phone.Contains(term)));
        }

        var suppliers = await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);

        return suppliers.Select(s => new SupplierDto
        {
            Id = s.Id,
            TenantId = s.TenantId,
            Name = s.Name,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            TaxNumber = s.TaxNumber,
            Notes = s.Notes,
            IsActive = s.IsActive
        }).ToList();
    }
}
