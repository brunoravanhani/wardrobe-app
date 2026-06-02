using Microsoft.EntityFrameworkCore;
using VirtualWardrobe.Application.Auth;
using VirtualWardrobe.Infrastructure.Persistence;
using VirtualWardrobe.Infrastructure.Persistence.Entities;

namespace VirtualWardrobe.Infrastructure.Auth;

public sealed class EfUserIdentityStore : IUserIdentityStore
{
    private readonly VirtualWardrobeDbContext _dbContext;

    public EfUserIdentityStore(VirtualWardrobeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthenticatedUser> GetOrCreateAsync(GoogleIdentityProfile profile, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(
            x => x.GoogleSubject == profile.Subject,
            cancellationToken);

        var utcNow = DateTime.UtcNow;

        if (user is null)
        {
            user = new UserRecord
            {
                Id = Guid.NewGuid(),
                GoogleSubject = profile.Subject,
                Email = profile.Email,
                DisplayName = profile.DisplayName,
                Locale = "pt-BR",
                CreatedAtUtc = utcNow,
                UpdatedAtUtc = utcNow
            };

            await _dbContext.Users.AddAsync(user, cancellationToken);
        }
        else
        {
            user.Email = profile.Email;
            user.DisplayName = profile.DisplayName;
            user.UpdatedAtUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthenticatedUser(
            user.Id,
            user.GoogleSubject,
            user.Email,
            user.DisplayName,
            user.Locale);
    }
}