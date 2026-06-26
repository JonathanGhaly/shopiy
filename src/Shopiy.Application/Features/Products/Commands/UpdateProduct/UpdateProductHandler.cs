using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.Common.Exceptions;
using Shopiy.Application.DTOs.Products;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace Shopiy.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductHandler
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateProductHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.ProductCategories)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.Id);
        }

        var slug = GenerateSlug(request.Name);

        var slugExists = await _context.Products
            .AnyAsync(p => p.Slug == slug && p.Id != request.Id, cancellationToken);

        if (slugExists)
        {
            throw new ConflictException($"Product with slug '{slug}' already exists.");
        }

        // Verify categories exist
        if (request.CategoryIds.Any())
        {
            var existingCategoriesCount = await _context.Categories
                .CountAsync(c => request.CategoryIds.Contains(c.Id), cancellationToken);

            if (existingCategoriesCount != request.CategoryIds.Count)
            {
                throw new NotFoundException("One or more category IDs do not exist.");
            }
        }

        product.Name = request.Name.Trim();
        product.Slug = slug;
        product.Description = request.Description.Trim();
        product.Price = (int)request.Price; // Request price is in cents
        product.StockQuantity = request.StockQuantity;
        product.MarkUpdated();

        // Sync Categories
        product.ProductCategories.Clear();
        foreach (var categoryId in request.CategoryIds)
        {
            product.ProductCategories.Add(new ProductCategory
            {
                ProductId = product.Id,
                CategoryId = categoryId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Fetch product with categories included for mapping
        var savedProduct = await _context.Products
            .Include(p => p.ProductCategories)
            .ThenInclude(pc => pc.Category)
            .FirstAsync(p => p.Id == product.Id, cancellationToken);

        return _mapper.Map<ProductDto>(savedProduct);
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return slug;
    }
}
