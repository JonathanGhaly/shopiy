namespace Shopiy.Application.DTOs.Authentication;

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}