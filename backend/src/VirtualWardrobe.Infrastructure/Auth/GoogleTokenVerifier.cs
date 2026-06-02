using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using VirtualWardrobe.Application.Auth;

namespace VirtualWardrobe.Infrastructure.Auth;

public sealed class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private readonly GoogleAuthOptions _options;

    public GoogleTokenVerifier(IOptions<GoogleAuthOptions> options)
    {
        _options = options.Value;
    }

    public async Task<GoogleIdentityProfile> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new InvalidOperationException("Auth:Google:ClientId must be configured.");
        }

        var payload = await GoogleJsonWebSignature.ValidateAsync(
            idToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_options.ClientId]
            });

        return new GoogleIdentityProfile(
            payload.Subject,
            payload.Email,
            payload.Name);
    }
}