using Shopiy.Domain.Common;

namespace Shopiy.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Price { get; set; }

    public string Currency { get; set; } = "EGP";

    public string? SKU { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public string Metadata { get; set; } = "{}";

    public ICollection<ProductCategory> ProductCategories { get; set; }
        = new List<ProductCategory>();

    public ICollection<OrderItem> OrderItems { get; set; }
        = new List<OrderItem>();
}