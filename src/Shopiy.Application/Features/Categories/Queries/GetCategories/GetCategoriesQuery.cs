using MediatR;
using Shopiy.Application.DTOs.Categories;
using Shopiy.Application.Behaviors;
using System.Collections.Generic;

namespace Shopiy.Application.Features.Categories.Queries.GetCategories;

[Cache(120)]
public sealed record GetCategoriesQuery() : IRequest<IReadOnlyList<CategoryDto>>;
