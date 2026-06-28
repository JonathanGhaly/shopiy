using System;
using System.Collections.Generic;

namespace Shopiy.Application.DTOs.Products;

public sealed class CreateProductRequest
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int StockQuantity { get; init; }

    public string? SKU { get; init; }

    public string? Currency { get; init; }

    public bool? IsActive { get; init; }

    public IDictionary<string, object>? Metadata { get; init; }

    public ICollection<Guid> CategoryIds { get; init; } = [];
}