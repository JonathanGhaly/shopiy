using MediatR;
using Shopiy.Application.DTOs.Products;
using Shopiy.Application.Common.Models;

namespace Shopiy.Application.Features.Products.Queries.GetProducts;

public sealed record GetProductsQuery(
    int Page = 1,
    int Limit = 20,
    string? Sort = null,
    Guid? CategoryId = null
) : IRequest<PaginatedResult<ProductDto>>;