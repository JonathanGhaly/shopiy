using MediatR;
using Shopiy.Application.DTOs.Categories;

namespace Shopiy.Application.Features.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string Description,
    Guid? ParentId = null,
    int SortOrder = 0
) : IRequest<CategoryDto>;
