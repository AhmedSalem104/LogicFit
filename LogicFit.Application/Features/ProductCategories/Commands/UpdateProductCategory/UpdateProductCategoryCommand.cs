using MediatR;

namespace LogicFit.Application.Features.ProductCategories.Commands.UpdateProductCategory;

public class UpdateProductCategoryCommand : IRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
