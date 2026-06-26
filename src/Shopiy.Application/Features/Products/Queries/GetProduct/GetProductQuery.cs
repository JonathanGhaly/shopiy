using MediatR;
using Shopiy.Application.DTOs.Products;

namespace Shopiy.Application.Features.Products.Queries.GetProduct;

public sealed record GetProductQuery(
    string SlugOrId
) : IRequest<ProductDto>;