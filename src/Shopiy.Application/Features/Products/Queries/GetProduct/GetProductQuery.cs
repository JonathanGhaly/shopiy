using MediatR;
using Shopiy.Application.DTOs.Products;
using Shopiy.Application.Behaviors;

namespace Shopiy.Application.Features.Products.Queries.GetProduct;

[Cache(120)]
public sealed record GetProductQuery(
    string SlugOrId
) : IRequest<ProductDto>;