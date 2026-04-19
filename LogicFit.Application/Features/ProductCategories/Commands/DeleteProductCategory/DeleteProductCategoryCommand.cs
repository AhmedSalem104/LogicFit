using MediatR;

namespace LogicFit.Application.Features.ProductCategories.Commands.DeleteProductCategory;

public class DeleteProductCategoryCommand : IRequest
{
    public Guid Id { get; set; }
}
