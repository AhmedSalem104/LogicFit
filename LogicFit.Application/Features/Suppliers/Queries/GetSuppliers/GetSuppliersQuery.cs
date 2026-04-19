using LogicFit.Application.Features.Suppliers.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Suppliers.Queries.GetSuppliers;

public class GetSuppliersQuery : IRequest<List<SupplierDto>>
{
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
}
