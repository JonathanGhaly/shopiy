using Shopiy.Domain.Entities;

namespace Shopiy.Domain.Interfaces;

public interface IRefreshTokenService
{
    Task<RefreshToken> GenerateAsync(Guid userId);

    Task RevokeAsync(Guid tokenId);

    Task<RefreshToken?> ValidateAsync(string token);
}