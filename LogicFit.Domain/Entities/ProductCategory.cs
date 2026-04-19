using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

public class ProductCategory : TenantAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ProductCategory? ParentCategory { get; set; }
    public virtual ICollection<ProductCategory> Children { get; set; } = new List<ProductCategory>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
