using LogicFit.Application.Features.Products.DTOs;
using MediatR;

namespace LogicFit.Application.Features.ProductCategories.Queries.GetProductCategories;

public class GetProductCategoriesQuery : IRequest<List<ProductCategoryDto>>
{
    public bool? IsActive { get; set; }
}
