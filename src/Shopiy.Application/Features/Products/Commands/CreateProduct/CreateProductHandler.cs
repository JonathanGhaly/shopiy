using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.Common.Exceptions;
using Shopiy.Application.DTOs.Products;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace Shopiy.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductHandler
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateProductHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var slug = GenerateSlug(request.Name);

        var slugExists = await _context.Products
            .AnyAsync(p => p.Slug == slug, cancellationToken);

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

        var product = new Product
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description.Trim(),
            Price = (int)request.Price, // Request price is in cents
            StockQuantity = request.StockQuantity,
            IsActive = true,
            Metadata = "{}"
        };

        foreach (var categoryId in request.CategoryIds)
        {
            product.ProductCategories.Add(new ProductCategory
            {
                CategoryId = categoryId
            });
        }

        _context.Products.Add(product);
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
        // Remove invalid characters
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        // Replace spaces/hyphens with single hyphens
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return slug;
    }
}
