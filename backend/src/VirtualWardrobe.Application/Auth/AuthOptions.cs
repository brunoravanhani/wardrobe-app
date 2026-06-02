namespace VirtualWardrobe.Application.Auth;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "Auth:Google";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "virtual-wardrobe-api";

    public string Audience { get; set; } = "virtual-wardrobe-client";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;
}