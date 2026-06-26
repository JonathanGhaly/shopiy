using MediatR;
using Shopiy.Application.DTOs.Authentication;

namespace Shopiy.Application.Features.Authentication.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken
) : IRequest<AuthResponse>;