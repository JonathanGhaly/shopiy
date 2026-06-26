using MediatR;
using Shopiy.Application.DTOs.Products;

namespace Shopiy.Application.Features.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    ICollection<Guid> CategoryIds
) : IRequest<ProductDto>;