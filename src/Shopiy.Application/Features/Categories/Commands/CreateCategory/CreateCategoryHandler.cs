using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.Common.Exceptions;
using Shopiy.Application.DTOs.Categories;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace Shopiy.Application.Features.Categories.Commands.CreateCategory;

public sealed class CreateCategoryHandler
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public CreateCategoryHandler(
        IApplicationDbContext context,
        IMapper mapper,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<CategoryDto> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var slug = GenerateSlug(request.Name);

        var slugExists = await _context.Categories
            .AnyAsync(c => c.Slug == slug, cancellationToken);

        if (slugExists)
        {
            throw new ConflictException($"Category with slug '{slug}' already exists.");
        }

        if (request.ParentId.HasValue)
        {
            var parentExists = await _context.Categories
                .AnyAsync(c => c.Id == request.ParentId.Value, cancellationToken);

            if (!parentExists)
            {
                throw new NotFoundException("Parent category does not exist.");
            }
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description.Trim(),
            ParentId = request.ParentId,
            SortOrder = request.SortOrder,
            CreatedBy = _currentUserService.UserId ?? Guid.Empty
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDto>(category);
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return slug;
    }
}
