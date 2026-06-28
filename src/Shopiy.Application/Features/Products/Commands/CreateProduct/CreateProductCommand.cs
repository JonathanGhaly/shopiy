using MediatR;
using Shopiy.Application.DTOs.Products;
using System;
using System.Collections.Generic;

namespace Shopiy.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    ICollection<Guid> CategoryIds,
    string? SKU,
    string? Currency,
    bool? IsActive,
    IDictionary<string, object>? Metadata
) : IRequest<ProductDto>;