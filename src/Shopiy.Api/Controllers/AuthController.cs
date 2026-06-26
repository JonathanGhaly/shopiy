using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shopiy.Application.DTOs.Authentication;
using Shopiy.Application.Features.Authentication.Commands.Login;
using Shopiy.Application.Features.Authentication.Commands.Logout;
using Shopiy.Application.Features.Authentication.Commands.RefreshToken;
using Shopiy.Application.Features.Authentication.Commands.Register;

namespace Shopiy.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Tags("Authentication")]
public class AuthController : ApiControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";
    private static readonly CookieOptions SecureCookieOptions = new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        Secure = true,
        Expires = DateTimeOffset.UtcNow.AddDays(7)
    };

    // ──────────────────────────
    // POST /api/v1/auth/register
    // ──────────────────────────
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.FullName,
            request.Email,
            request.Password,
            request.ConfirmPassword);

        var result = await Sender.Send(command, cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken);

        return CreatedAtAction(
            nameof(Register),
            new { },
            new AuthResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = string.Empty,
                User = result.User
            });
    }

    // ──────────────────────────
    // POST /api/v1/auth/login
    // ──────────────────────────
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await Sender.Send(command, cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(new AuthResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = string.Empty,
            User = result.User
        });
    }

    // ──────────────────────────
    // POST /api/v1/auth/refresh
    // ──────────────────────────
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(new { error = new { code = "AUTH_REQUIRED", message = "Refresh token not found." } });

        var command = new RefreshTokenCommand(refreshToken);
        var result = await Sender.Send(command, cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(new AuthResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = string.Empty,
            User = result.User
        });
    }

    // ──────────────────────────
    // POST /api/v1/auth/logout
    // ──────────────────────────
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await Sender.Send(new LogoutCommand(refreshToken), cancellationToken);
        }

        Response.Cookies.Delete(RefreshTokenCookieName);

        return NoContent();
    }

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = true,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
    }
}
