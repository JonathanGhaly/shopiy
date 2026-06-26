namespace Shopiy.Application.DTOs.Products;

public sealed class ProductDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int StockQuantity { get; init; }

    public IReadOnlyCollection<string> Categories { get; init; } = [];
}