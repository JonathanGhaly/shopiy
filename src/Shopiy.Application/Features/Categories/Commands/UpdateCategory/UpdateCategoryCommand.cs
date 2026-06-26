using MediatR;
using Shopiy.Application.DTOs.Categories;

namespace Shopiy.Application.Features.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string Description,
    Guid? ParentId = null,
    int SortOrder = 0
) : IRequest<CategoryDto>;
