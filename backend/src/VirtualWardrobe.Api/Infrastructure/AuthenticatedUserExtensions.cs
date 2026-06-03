using System.Security.Claims;

namespace VirtualWardrobe.Api.Infrastructure;

public static class AuthenticatedUserExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException("Authenticated user id is missing.");
        }

        return userId;
    }
}
