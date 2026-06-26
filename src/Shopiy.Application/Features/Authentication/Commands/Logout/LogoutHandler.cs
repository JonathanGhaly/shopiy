using MediatR;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Application.Features.Authentication.Commands.Logout;

public sealed class LogoutHandler
    : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutHandler(
        IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Unit> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        Shopiy.Domain.Entities.RefreshToken? token =
            await _refreshTokenService.ValidateAsync(request.RefreshToken);

        if (token is null)
            return Unit.Value;

        await _refreshTokenService.RevokeAsync(token.Id);

        return Unit.Value;
    }
}