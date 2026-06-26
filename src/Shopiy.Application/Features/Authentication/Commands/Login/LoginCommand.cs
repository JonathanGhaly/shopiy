using MediatR;
using Shopiy.Application.DTOs.Authentication;

namespace Shopiy.Application.Features.Authentication.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthResponse>;