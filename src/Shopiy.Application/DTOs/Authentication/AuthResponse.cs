namespace Shopiy.Application.DTOs.Authentication;

public sealed record AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }

    public UserDto User { get; init; } = null!;
}