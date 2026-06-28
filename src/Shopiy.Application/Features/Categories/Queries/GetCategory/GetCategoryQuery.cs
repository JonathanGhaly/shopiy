using MediatR;
using Shopiy.Application.DTOs.Categories;
using Shopiy.Application.Behaviors;

namespace Shopiy.Application.Features.Categories.Queries.GetCategory;

[Cache(120)]
public sealed record GetCategoryQuery(
    string SlugOrId
) : IRequest<CategoryDto>;
