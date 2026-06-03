using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualWardrobe.Application.Auth;

namespace VirtualWardrobe.Api.Controllers;

[ApiController]
[Route("v1/auth/google")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthSessionService _authSessionService;

    public AuthController(AuthSessionService authSessionService)
    {
        _authSessionService = authSessionService;
    }

    [HttpPost("exchange")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthSessionResponse>> ExchangeAsync(
        [FromBody] ExchangeGoogleTokenRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _authSessionService.ExchangeGoogleTokenAsync(request.IdToken, cancellationToken);

        return Ok(new AuthSessionResponse(
            session.AccessToken,
            session.ExpiresAtUtc,
            new AuthenticatedUserResponse(
                session.User.UserId,
                session.User.Email,
                session.User.DisplayName,
                session.User.Locale)));
    }
}

public sealed record ExchangeGoogleTokenRequest(string IdToken);

public sealed record AuthSessionResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    AuthenticatedUserResponse User);

public sealed record AuthenticatedUserResponse(
    Guid UserId,
    string Email,
    string? DisplayName,
    string Locale);
