namespace Shopiy.Application.DTOs.Authentication;

public sealed class UserDto
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public IList<string> Roles { get; init; } = [];
}