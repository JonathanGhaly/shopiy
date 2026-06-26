using MediatR;

namespace Shopiy.Application.Features.Authentication.Commands.Logout;

public sealed record LogoutCommand(
    string RefreshToken
) : IRequest<Unit>;