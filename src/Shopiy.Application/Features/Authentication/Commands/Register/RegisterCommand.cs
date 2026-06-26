using MediatR;
using Shopiy.Application.DTOs.Authentication;

namespace Shopiy.Application.Features.Authentication.Commands.Register;

public sealed record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword
) : IRequest<AuthResponse>;