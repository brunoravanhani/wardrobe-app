using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wishlist;

namespace VirtualWardrobe.Application.Wishlist;

public interface IWishlistItemRepository
{
    Task AddAsync(WishlistItem item, CancellationToken cancellationToken);

    Task UpdateAsync(WishlistItem item, CancellationToken cancellationToken);

    Task<WishlistItem?> GetByIdAsync(WishlistItemId itemId, UserId ownerUserId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WishlistItem>> ListAsync(UserId ownerUserId, bool includePurchased, CancellationToken cancellationToken);

    Task RemoveAsync(WishlistItem item, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
