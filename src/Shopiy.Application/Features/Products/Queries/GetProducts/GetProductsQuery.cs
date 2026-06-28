using MediatR;
using Shopiy.Application.DTOs.Products;
using Shopiy.Application.Common.Models;
using Shopiy.Application.Behaviors;
using System;

namespace Shopiy.Application.Features.Products.Queries.GetProducts;

[Cache(120)]
public sealed record GetProductsQuery(
    int Page = 1,
    int Limit = 20,
    string? Sort = null,
    Guid? CategoryId = null,
    string? Search = null
) : IRequest<PaginatedResult<ProductDto>>;