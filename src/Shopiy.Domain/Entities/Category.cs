using Shopiy.Domain.Common;

namespace Shopiy.Domain.Entities;

/// <summary>
/// Represents a product category in the catalogue hierarchy.
/// Categories support a single level of nesting via <see cref="ParentId"/>.
/// </summary>
public class Category : BaseEntity
{
    /// <summary>Human-readable category name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug derived from <see cref="Name"/>.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Optional description shown on category landing pages.</summary>
    public string? Description { get; set; }

    /// <summary>Foreign key to the parent <see cref="Category"/>. <c>null</c> for root categories.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Navigation property to the parent category.</summary>
    public Category? Parent { get; set; }

    /// <summary>Direct child categories.</summary>
    public ICollection<Category> Children { get; set; }
        = new List<Category>();

    /// <summary>Display order relative to sibling categories. Lower values appear first.</summary>
    public int SortOrder { get; set; }

    /// <summary>ID of the admin user who created this category.</summary>
    public Guid CreatedBy { get; set; }

    /// <summary>Join table entries linking this category to its products.</summary>
    public ICollection<ProductCategory> ProductCategories { get; set; }
        = new List<ProductCategory>();
}