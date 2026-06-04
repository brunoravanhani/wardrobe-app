using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VirtualWardrobe.Api.Controllers;
using VirtualWardrobe.Application.Wishlist;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wishlist;

namespace VirtualWardrobe.ContractTests.Wishlist;

public sealed class WishlistContractTests
{
    [Fact]
    public async Task WishlistCrudContractShouldSupportCreateListUpdateDelete()
    {
        var ownerUserId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();

        var mediaRepository = new InMemoryMediaAssetRepository(ownerUserId, mediaId);
        var wishlistRepository = new InMemoryWishlistItemRepository();
        var command = new CreateWishlistItemCommand(wishlistRepository, mediaRepository);

        var controller = new WishlistItemsController(command);
        AttachUser(controller, ownerUserId);

        var createAction = await controller.CreateAsync(
            new CreateWishlistItemRequest(
                ClothingCategory.Coats,
                "Jaqueta",
                "Marca",
                280m,
                mediaId,
                ["https://shop.example.com/items/jaqueta"]),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(createAction.Result);
        var createdItem = Assert.IsType<WishlistItemResponse>(created.Value);

        var updateAction = await controller.UpdateAsync(
            createdItem.Id,
            new UpdateWishlistItemRequest(
                ClothingCategory.Coats,
                "Jaqueta inverno",
                "Outra",
                300m,
                mediaId,
                ["https://shop.example.com/items/jaqueta-2"]),
            CancellationToken.None);

        var updated = Assert.IsType<OkObjectResult>(updateAction.Result);
        var updatedPayload = Assert.IsType<WishlistItemResponse>(updated.Value);
        Assert.Equal("Jaqueta inverno", updatedPayload.Name);

        var listAction = await controller.ListAsync(false, CancellationToken.None);
        var listed = Assert.IsType<OkObjectResult>(listAction.Result);
        var listPayload = Assert.IsType<WishlistItemResponse[]>(listed.Value);
        Assert.Single(listPayload);

        var deleteAction = await controller.DeleteAsync(createdItem.Id, CancellationToken.None);
        Assert.IsType<NoContentResult>(deleteAction);
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

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
