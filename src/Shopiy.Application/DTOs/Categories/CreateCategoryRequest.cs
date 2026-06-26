namespace Shopiy.Application.DTOs.Categories;

public sealed class CreateCategoryRequest
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}