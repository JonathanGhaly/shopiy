using Shopiy.Domain.Common;

namespace Shopiy.Domain.Entities;

/// <summary>
/// Represents a purchasable item in the catalogue.
/// Prices are stored as integers in the smallest currency unit (piastres for EGP).
/// </summary>
public class Product : BaseEntity
{
    /// <summary>Display name of the product.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly identifier derived from <see cref="Name"/>.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Full description of the product.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Price in the smallest currency unit (piastres). Divide by 100 for display.</summary>
    public int Price { get; set; }

    /// <summary>ISO 4217 currency code. Defaults to <c>EGP</c>.</summary>
    public string Currency { get; set; } = "EGP";

    /// <summary>Optional Stock-Keeping Unit code.</summary>
    public string? SKU { get; set; }

    /// <summary>Number of units currently available in inventory.</summary>
    public int StockQuantity { get; set; }

    /// <summary>Whether this product is visible and purchasable. Soft-disabled products are hidden from public queries.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>JSON blob for arbitrary key/value metadata (e.g. dimensions, colour).</summary>
    public string Metadata { get; set; } = "{}";

    /// <summary>Many-to-many join to <see cref="Category"/> through <see cref="ProductCategory"/>.</summary>
    public ICollection<ProductCategory> ProductCategories { get; set; }
        = new List<ProductCategory>();

    /// <summary>Line items that reference this product in placed orders.</summary>
    public ICollection<OrderItem> OrderItems { get; set; }
        = new List<OrderItem>();
}