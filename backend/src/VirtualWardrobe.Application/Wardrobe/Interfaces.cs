using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wardrobe;

namespace VirtualWardrobe.Application.Wardrobe;

public interface IWardrobeItemRepository
{
    Task AddAsync(WardrobeItem item, CancellationToken cancellationToken);

    Task UpdateAsync(WardrobeItem item, CancellationToken cancellationToken);

    Task<WardrobeItem?> GetByIdAsync(WardrobeItemId itemId, UserId ownerUserId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WardrobeItem>> ListAsync(UserId ownerUserId, ClothingCategory? category, CancellationToken cancellationToken);

    Task RemoveAsync(WardrobeItem item, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IMediaAssetRepository
{
    Task<bool> ExistsOwnedByAsync(MediaAssetId mediaAssetId, UserId ownerUserId, CancellationToken cancellationToken);
}
