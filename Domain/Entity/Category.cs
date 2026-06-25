using Shopiy.Domain.Common;

namespace Shopiy.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }

    public Category? Parent { get; set; }

    public ICollection<Category> Children { get; set; }
        = new List<Category>();

    public int SortOrder { get; set; }

    public Guid CreatedBy { get; set; }

    public ICollection<ProductCategory> ProductCategories { get; set; }
        = new List<ProductCategory>();
}