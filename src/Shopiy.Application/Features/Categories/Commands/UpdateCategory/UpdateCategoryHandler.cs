using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.Common.Exceptions;
using Shopiy.Application.DTOs.Categories;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace Shopiy.Application.Features.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryHandler
    : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateCategoryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<CategoryDto> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(nameof(Category), request.Id);
        }

        var slug = GenerateSlug(request.Name);

        var slugExists = await _context.Categories
            .AnyAsync(c => c.Slug == slug && c.Id != request.Id, cancellationToken);

        if (slugExists)
        {
            throw new ConflictException($"Category with slug '{slug}' already exists.");
        }

        if (request.ParentId.HasValue)
        {
            await VerifyNotCircularAsync(request.Id, request.ParentId.Value, cancellationToken);

            var parentExists = await _context.Categories
                .AnyAsync(c => c.Id == request.ParentId.Value, cancellationToken);

            if (!parentExists)
            {
                throw new NotFoundException("Parent category does not exist.");
            }
        }

        category.Name = request.Name.Trim();
        category.Slug = slug;
        category.Description = request.Description.Trim();
        category.ParentId = request.ParentId;
        category.SortOrder = request.SortOrder;
        category.MarkUpdated();

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDto>(category);
    }

    private async Task VerifyNotCircularAsync(Guid categoryId, Guid parentId, CancellationToken cancellationToken)
    {
        if (categoryId == parentId)
        {
            throw new ConflictException("Category cannot reference itself as its parent.");
        }

        var currentParentId = (Guid?)parentId;
        while (currentParentId.HasValue)
        {
            var parent = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == currentParentId.Value, cancellationToken);

            if (parent is null)
                break;

            if (parent.ParentId == categoryId)
            {
                throw new ConflictException("Circular dependency detected in parent category hierarchy.");
            }

            currentParentId = parent.ParentId;
        }
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return slug;
    }
}
