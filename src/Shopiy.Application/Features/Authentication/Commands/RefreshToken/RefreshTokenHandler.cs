using MediatR;
using Microsoft.AspNetCore.Identity;
using Shopiy.Application.DTOs.Authentication;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Application.Features.Authentication.Commands.RefreshToken;

public sealed class RefreshTokenHandler
    : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;

    public RefreshTokenHandler(
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
         Shopiy.Domain.Entities.RefreshToken? storedToken =
            await _refreshTokenService.ValidateAsync(request.RefreshToken);

        if (storedToken is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var user = await _userManager.FindByIdAsync(
            storedToken.UserId.ToString());

        if (user is null)
            throw new UnauthorizedAccessException();

        await _refreshTokenService.RevokeAsync(tokenId: storedToken.Id);

        var newRefreshToken =
            await _refreshTokenService.GenerateAsync(user.Id);

        var accessToken =
            await _jwtService.GenerateTokenAsync(user.Id, user.Email!, await _userManager.GetRolesAsync(user));

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.PlainTextToken!,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),

            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Roles = roles
            }
        };
    }
}