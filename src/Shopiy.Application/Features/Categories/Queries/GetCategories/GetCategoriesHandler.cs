using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.DTOs.Categories;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Application.Features.Categories.Queries.GetCategories;

public sealed class GetCategoriesHandler
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCategoriesHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<CategoryDto>>(categories);
    }
}
