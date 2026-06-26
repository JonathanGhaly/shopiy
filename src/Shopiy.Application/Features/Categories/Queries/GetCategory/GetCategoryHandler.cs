using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.Common.Exceptions;
using Shopiy.Application.DTOs.Categories;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Application.Features.Categories.Queries.GetCategory;

public sealed class GetCategoryHandler
    : IRequestHandler<GetCategoryQuery, CategoryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCategoryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<CategoryDto> Handle(
        GetCategoryQuery request,
        CancellationToken cancellationToken)
    {
        Category? category = null;

        if (Guid.TryParse(request.SlugOrId, out var id))
        {
            category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        if (category is null)
        {
            category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == request.SlugOrId, cancellationToken);
        }

        if (category is null)
        {
            throw new NotFoundException(nameof(Category), request.SlugOrId);
        }

        return _mapper.Map<CategoryDto>(category);
    }
}
