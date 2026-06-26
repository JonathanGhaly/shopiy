using MediatR;
using Shopiy.Application.DTOs.Products;

namespace Shopiy.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    ICollection<Guid> CategoryIds
) : IRequest<ProductDto>;