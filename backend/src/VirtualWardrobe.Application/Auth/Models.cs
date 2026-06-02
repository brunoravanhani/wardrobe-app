namespace VirtualWardrobe.Application.Auth;

public sealed record GoogleIdentityProfile(
    string Subject,
    string Email,
    string? DisplayName
);

public sealed record AuthenticatedUser(
    Guid UserId,
    string GoogleSubject,
    string Email,
    string? DisplayName,
    string Locale
);

public sealed record AuthSession(
    string AccessToken,
    DateTime ExpiresAtUtc,
    AuthenticatedUser User
);