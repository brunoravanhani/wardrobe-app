using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using VirtualWardrobe.Api.Controllers;
using VirtualWardrobe.Application.Storage;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Application.Wishlist;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wardrobe;
using VirtualWardrobe.Domain.Wishlist;

namespace VirtualWardrobe.ContractTests.Wishlist;

public sealed class WishlistConversionContractTests
{
    [Fact]
    public async Task PurchaseAndConvertContractShouldReturnIdempotentWardrobeItem()
    {
        var ownerUserId = Guid.NewGuid();

        var mediaRepository = new InMemoryMediaAssetRepository(ownerUserId);
        var wishlistRepository = new InMemoryWishlistItemRepository();
        var wardrobeRepository = new InMemoryWardrobeItemRepository();
        var wishlistCommand = new CreateWishlistItemCommand(wishlistRepository, mediaRepository, new NoOpMediaUrlService());
        var conversionCommand = new ConvertWishlistItemCommand(wishlistRepository, wardrobeRepository, mediaRepository);

        var controller = new WishlistItemsController(wishlistCommand, conversionCommand, NullLogger<WishlistItemsController>.Instance);
        AttachUser(controller, ownerUserId);

        var createAction = await controller.CreateAsync(
            new CreateWishlistItemRequest(
                "Shirt",
                "Camisa casual",
                "Marca D",
                150m,
                null,
                ["https://shop.example.com/items/camisa-casual"]),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(createAction.Result);
        var createdItem = Assert.IsType<WishlistItemResponse>(created.Value);

        var purchaseAction = await controller.MarkAsPurchasedAsync(createdItem.Id, CancellationToken.None);
        var purchased = Assert.IsType<OkObjectResult>(purchaseAction.Result);
        var purchasedPayload = Assert.IsType<WishlistItemResponse>(purchased.Value);
        Assert.Equal(WishlistItemStatus.Purchased, purchasedPayload.Status);

        var firstConvert = await controller.ConvertToWardrobeAsync(
            createdItem.Id,
            new ConvertWishlistItemRequest(
                null,
                null,
                "M",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var secondConvert = await controller.ConvertToWardrobeAsync(
            createdItem.Id,
            new ConvertWishlistItemRequest(
                null,
                null,
                "M",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var firstConvertedResult = Assert.IsType<OkObjectResult>(firstConvert.Result);
        var firstConvertedPayload = Assert.IsType<WishlistConversionResponse>(firstConvertedResult.Value);

        var secondConvertedResult = Assert.IsType<OkObjectResult>(secondConvert.Result);
        var secondConvertedPayload = Assert.IsType<WishlistConversionResponse>(secondConvertedResult.Value);

        Assert.Equal(firstConvertedPayload.WardrobeItem.Id, secondConvertedPayload.WardrobeItem.Id);
        Assert.Equal(createdItem.Id, firstConvertedPayload.WishlistItemId);
    }

    private static void AttachUser(ControllerBase controller, Guid userId)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    ],
                    authenticationType: "test"))
            }
        };
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

    private sealed class InMemoryWishlistItemRepository : IWishlistItemRepository
    {
        private readonly Dictionary<Guid, WishlistItem> _items = [];

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

    private sealed class NoOpMediaUrlService : IPrivateMediaUrlService
    {
        public Task<PresignedUploadResult> CreateUploadUrlAsync(PresignedUploadRequest request, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<PresignedViewResult> CreateViewUrlAsync(Guid mediaAssetId, Guid ownerUserId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task DeleteMediaAssetAsync(Guid mediaAssetId, Guid ownerUserId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}