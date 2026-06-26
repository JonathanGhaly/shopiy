using MediatR;
using Shopiy.Application.DTOs.Categories;

namespace Shopiy.Application.Features.Categories.Queries.GetCategories;

public sealed record GetCategoriesQuery() : IRequest<IReadOnlyList<CategoryDto>>;
