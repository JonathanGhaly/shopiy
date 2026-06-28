using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.Common.Exceptions;
using Shopiy.Application.DTOs.Products;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Application.Features.Products.Queries.GetProduct;

public sealed class GetProductHandler
    : IRequestHandler<GetProductQuery, ProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public GetProductHandler(IApplicationDbContext context, IMapper mapper, ICurrentUserService currentUserService)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<ProductDto> Handle(
        GetProductQuery request,
        CancellationToken cancellationToken)
    {
        Product? product = null;

        if (Guid.TryParse(request.SlugOrId, out var id))
        {
            product = await _context.Products
                .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        if (product is null)
        {
            product = await _context.Products
                .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Slug == request.SlugOrId, cancellationToken);
        }

        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.SlugOrId);
        }

        var isAdmin = _currentUserService.Roles.Contains("Admin");
        if (!product.IsActive && !isAdmin)
        {
            throw new NotFoundException(nameof(Product), request.SlugOrId);
        }

        return _mapper.Map<ProductDto>(product);
    }
}
