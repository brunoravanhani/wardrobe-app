using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Application.Wishlist;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wardrobe;
using VirtualWardrobe.Domain.Wishlist;

namespace VirtualWardrobe.UnitTests.Wishlist;

public sealed class WishlistConversionTests
{
    [Fact]
    public async Task MarkAsPurchasedShouldSetPurchasedStatus()
    {
        var ownerId = Guid.NewGuid();
        var item = WishlistItem.Create(
            WishlistItemId.New(),
            new UserId(ownerId),
            ClothingCategory.Shirt,
            "Camisa premium",
            "Marca X",
            199.90m,
            null,
            [("https://shop.example.com/items/1", null)]);

        var wishlistRepository = new InMemoryWishlistItemRepository(item);
        var wardrobeRepository = new InMemoryWardrobeItemRepository();
        var mediaRepository = new InMemoryMediaAssetRepository(ownerId);
        var command = new ConvertWishlistItemCommand(wishlistRepository, wardrobeRepository, mediaRepository);

        var result = await command.MarkAsPurchasedAsync(item.Id.Value, ownerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(WishlistItemStatus.Purchased, result.Value.Status);
        Assert.NotNull(result.Value.PurchasedAtUtc);
    }

    [Fact]
    public async Task ConvertWithoutSizeShouldFailValidation()
    {
        var ownerId = Guid.NewGuid();
        var item = WishlistItem.Create(
            WishlistItemId.New(),
            new UserId(ownerId),
            ClothingCategory.Coats,
            "Jaqueta",
            "Marca A",
            320m,
            null,
            [("https://shop.example.com/items/2", null)]);
        item.MarkAsPurchased();

        var wishlistRepository = new InMemoryWishlistItemRepository(item);
        var wardrobeRepository = new InMemoryWardrobeItemRepository();
        var mediaRepository = new InMemoryMediaAssetRepository(ownerId);
        var command = new ConvertWishlistItemCommand(wishlistRepository, wardrobeRepository, mediaRepository);

        var result = await command.ConvertToWardrobeAsync(
            new ConvertWishlistItemInput(
                item.Id.Value,
                ownerId,
                null,
                null,
                " ",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error.Code);
    }

    [Fact]
    public async Task ConvertPurchasedItemShouldBeIdempotent()
    {
        var ownerId = Guid.NewGuid();
        var item = WishlistItem.Create(
            WishlistItemId.New(),
            new UserId(ownerId),
            ClothingCategory.Shoes,
            "Tenis corrida",
            "Marca B",
            410m,
            null,
            [("https://shop.example.com/items/3", null)]);
        item.MarkAsPurchased();

        var wishlistRepository = new InMemoryWishlistItemRepository(item);
        var wardrobeRepository = new InMemoryWardrobeItemRepository();
        var mediaRepository = new InMemoryMediaAssetRepository(ownerId);
        var command = new ConvertWishlistItemCommand(wishlistRepository, wardrobeRepository, mediaRepository);

        var firstResult = await command.ConvertToWardrobeAsync(
            new ConvertWishlistItemInput(
                item.Id.Value,
                ownerId,
                null,
                null,
                "42",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var secondResult = await command.ConvertToWardrobeAsync(
            new ConvertWishlistItemInput(
                item.Id.Value,
                ownerId,
                null,
                null,
                "42",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.Equal(firstResult.Value.Id, secondResult.Value.Id);

        var persistedWishlist = await wishlistRepository.GetByIdAsync(item.Id, new UserId(ownerId), CancellationToken.None);
        Assert.NotNull(persistedWishlist);
        Assert.Equal(firstResult.Value.Id.Value, persistedWishlist!.ConvertedWardrobeItemId);
        Assert.Equal(1, wardrobeRepository.Count);
    }

    private sealed class InMemoryWishlistItemRepository : IWishlistItemRepository
    {
        private readonly Dictionary<Guid, WishlistItem> _items = [];

        public InMemoryWishlistItemRepository(params WishlistItem[] items)
        {
            foreach (var item in items)
            {
                _items[item.Id.Value] = item;
            }
        }

        public Task AddAsync(WishlistItem item, CancellationToken cancellationToken)
        {
            _items[item.Id.Value] = item;
            return Task.CompletedTask;
        }

        public Task<WishlistItem?> GetByIdAsync(WishlistItemId itemId, UserId ownerUserId, CancellationToken cancellationToken)
        {
            if (_items.TryGetValue(itemId.Value, out var item) && item.OwnerUserId == ownerUserId)
            {
                return Task.FromResult<WishlistItem?>(item);
            }

            return Task.FromResult<WishlistItem?>(null);
        }

        public Task<IReadOnlyList<WishlistItem>> ListAsync(UserId ownerUserId, bool includePurchased, CancellationToken cancellationToken)
        {
            var query = _items.Values.Where(x => x.OwnerUserId == ownerUserId);
            if (!includePurchased)
            {
                query = query.Where(x => x.Status != WishlistItemStatus.Purchased);
            }

            return Task.FromResult<IReadOnlyList<WishlistItem>>(query.ToArray());
        }

        public Task RemoveAsync(WishlistItem item, CancellationToken cancellationToken)
        {
            _items.Remove(item.Id.Value);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(WishlistItem item, CancellationToken cancellationToken)
        {
            _items[item.Id.Value] = item;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryWardrobeItemRepository : IWardrobeItemRepository
    {
        private readonly Dictionary<Guid, WardrobeItem> _items = [];

        public int Count => _items.Count;

        public Task AddAsync(WardrobeItem item, CancellationToken cancellationToken)
        {
            _items[item.Id.Value] = item;
            return Task.CompletedTask;
        }

        public Task<WardrobeItem?> GetByIdAsync(WardrobeItemId itemId, UserId ownerUserId, CancellationToken cancellationToken)
        {
            if (_items.TryGetValue(itemId.Value, out var item) && item.OwnerUserId == ownerUserId)
            {
                return Task.FromResult<WardrobeItem?>(item);
            }

            return Task.FromResult<WardrobeItem?>(null);
        }

        public Task<IReadOnlyList<WardrobeItem>> ListAsync(UserId ownerUserId, ClothingCategory? category, CancellationToken cancellationToken)
        {
            var query = _items.Values.Where(x => x.OwnerUserId == ownerUserId);
            if (category.HasValue)
            {
                query = query.Where(x => x.Category == category.Value);
            }

            return Task.FromResult<IReadOnlyList<WardrobeItem>>(query.ToArray());
        }

        public Task RemoveAsync(WardrobeItem item, CancellationToken cancellationToken)
        {
            _items.Remove(item.Id.Value);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(WardrobeItem item, CancellationToken cancellationToken)
        {
            _items[item.Id.Value] = item;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryMediaAssetRepository : IMediaAssetRepository
    {
        private readonly Guid _owner;
        private readonly HashSet<Guid> _ownedMediaIds;

        public InMemoryMediaAssetRepository(Guid owner, params Guid[] ownedMediaIds)
        {
            _owner = owner;
            _ownedMediaIds = ownedMediaIds.ToHashSet();
        }

        public Task<bool> ExistsOwnedByAsync(MediaAssetId mediaAssetId, UserId ownerUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ownerUserId.Value == _owner && _ownedMediaIds.Contains(mediaAssetId.Value));
        }
    }
}