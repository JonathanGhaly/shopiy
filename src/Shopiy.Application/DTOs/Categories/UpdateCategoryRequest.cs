namespace Shopiy.Application.DTOs.Categories;

public sealed class UpdateCategoryRequest
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}