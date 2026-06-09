using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualWardrobe.Api.Observability;
using VirtualWardrobe.Application.Auth;

namespace VirtualWardrobe.Api.Controllers;

[ApiController]
[Route("v1/auth/google")]
public sealed partial class AuthController : ControllerBase
{
    private readonly AuthSessionService _authSessionService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthSessionService authSessionService, ILogger<AuthController> logger)
    {
        _authSessionService = authSessionService;
        _logger = logger;
    }

    [HttpPost("exchange")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthSessionResponse>> ExchangeAsync(
        [FromBody] ExchangeGoogleTokenRequest request,
        CancellationToken cancellationToken)
    {
        TelemetryConfig.AuthExchangeTotal.Add(1);
        Log.AuthExchangeInitiated(_logger);

        try
        {
            var session = await _authSessionService.ExchangeGoogleTokenAsync(request.IdToken, cancellationToken);

            Log.AuthExchangeSucceeded(_logger, session.User.UserId);
            return Ok(new AuthSessionResponse(
                session.AccessToken,
                session.ExpiresAtUtc,
                new AuthenticatedUserResponse(
                    session.User.UserId,
                    session.User.Email,
                    session.User.DisplayName,
                    session.User.Locale)));
        }
        catch (Exception ex)
        {
            TelemetryConfig.AuthExchangeFailures.Add(1);
            Log.AuthExchangeFailed(_logger, ex);
            throw;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Auth exchange initiated")]
        internal static partial void AuthExchangeInitiated(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information, Message = "Auth exchange succeeded for user {UserId}")]
        internal static partial void AuthExchangeSucceeded(ILogger logger, Guid userId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Auth exchange failed")]
        internal static partial void AuthExchangeFailed(ILogger logger, Exception exception);
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
