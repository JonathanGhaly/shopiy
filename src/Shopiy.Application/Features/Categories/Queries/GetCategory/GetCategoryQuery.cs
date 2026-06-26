using MediatR;
using Shopiy.Application.DTOs.Categories;

namespace Shopiy.Application.Features.Categories.Queries.GetCategory;

public sealed record GetCategoryQuery(
    string SlugOrId
) : IRequest<CategoryDto>;
