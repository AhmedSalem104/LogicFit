using MediatR;

namespace LogicFit.Application.Features.ProductCategories.Commands.CreateProductCategory;

public class CreateProductCategoryCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
