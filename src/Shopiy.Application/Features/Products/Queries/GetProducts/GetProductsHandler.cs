using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.Common.Models;
using Shopiy.Application.DTOs.Products;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsHandler
    : IRequestHandler<GetProductsQuery, PaginatedResult<ProductDto>>
{
    private readonly IApplicationReadOnlyDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public GetProductsHandler(IApplicationReadOnlyDbContext context, IMapper mapper, ICurrentUserService currentUserService)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedResult<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var isAdmin = _currentUserService.Roles.Contains("Admin");

        var query = _context.Products
            .Include(p => p.ProductCategories)
            .ThenInclude(pc => pc.Category)
            .AsNoTracking()
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(p => p.IsActive);
        }

        // Category Filter
        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == request.CategoryId.Value));
        }

        // Sorting
        query = request.Sort?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderBy(p => p.Name)
        };

        // Total count
        var totalItems = await query.CountAsync(cancellationToken);

        // Pagination bounds
        var page = request.Page < 1 ? 1 : request.Page;
        var limit = request.Limit < 1 ? 20 : request.Limit > 100 ? 100 : request.Limit;

        var products = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var productDtos = _mapper.Map<List<ProductDto>>(products);

        return new PaginatedResult<ProductDto>(productDtos, totalItems, page, limit);
    }
}
