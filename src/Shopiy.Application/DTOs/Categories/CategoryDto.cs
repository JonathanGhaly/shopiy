namespace Shopiy.Application.DTOs.Categories;

public sealed class CategoryDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}