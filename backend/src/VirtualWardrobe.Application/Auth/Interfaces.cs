namespace VirtualWardrobe.Application.Auth;

public interface IGoogleTokenVerifier
{
    Task<GoogleIdentityProfile> VerifyAsync(string idToken, CancellationToken cancellationToken);
}

public interface IUserIdentityStore
{
    Task<AuthenticatedUser> GetOrCreateAsync(GoogleIdentityProfile profile, CancellationToken cancellationToken);
}

public interface IApiTokenIssuer
{
    AuthSession CreateSession(AuthenticatedUser user);
}