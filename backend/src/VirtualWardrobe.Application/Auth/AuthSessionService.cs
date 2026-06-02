using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace VirtualWardrobe.Application.Auth;

public sealed class AuthSessionService
{
    private readonly IGoogleTokenVerifier _googleTokenVerifier;
    private readonly IUserIdentityStore _userIdentityStore;
    private readonly JwtOptions _jwtOptions;

    public AuthSessionService(
        IGoogleTokenVerifier googleTokenVerifier,
        IUserIdentityStore userIdentityStore,
        IOptions<JwtOptions> jwtOptions)
    {
        _googleTokenVerifier = googleTokenVerifier;
        _userIdentityStore = userIdentityStore;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthSession> ExchangeGoogleTokenAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new ArgumentException("Google token is required.", nameof(idToken));
        }

        var profile = await _googleTokenVerifier.VerifyAsync(idToken, cancellationToken);
        var user = await _userIdentityStore.GetOrCreateAsync(profile, cancellationToken);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email)
        };

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            claims.Add(new Claim(ClaimTypes.Name, user.DisplayName));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthSession(accessToken, expiresAtUtc, user);
    }
}