using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;
using Shopiy.Infrastructure.Persistence;

namespace Shopiy.Infrastructure.Authentication;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenService(
        ApplicationDbContext context,
        Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<RefreshToken> GenerateAsync(Guid userId)
    {
        var rawToken = GenerateSecureToken();

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(
                _jwtOptions.RefreshTokenExpirationDays),
            Revoked = false
        };

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        // Store the plain token temporarily.
        // It will be written to the HttpOnly cookie
        // by the Authentication Controller.
        refreshToken.PlainTextToken = rawToken;

        return refreshToken;
    }

    public async Task<RefreshToken?> ValidateAsync(string token)
    {
        var hash = Hash(token);

        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x =>
                x.TokenHash == hash &&
                !x.Revoked);

        if (refreshToken is null)
            return null;

        if (refreshToken.ExpiresAt <= DateTime.UtcNow)
            return null;

        return refreshToken;
    }

    public async Task RevokeAsync(Guid tokenId)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Id == tokenId);

        if (refreshToken is null)
            return;

        refreshToken.Revoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(bytes);
    }

    private static string Hash(string token)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }
}