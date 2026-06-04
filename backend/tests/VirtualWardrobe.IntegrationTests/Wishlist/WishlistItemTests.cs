using Microsoft.EntityFrameworkCore;
using VirtualWardrobe.Application.Wishlist;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wishlist;
using VirtualWardrobe.Infrastructure.Persistence;
using VirtualWardrobe.Infrastructure.Persistence.Configurations;
using VirtualWardrobe.Infrastructure.Persistence.Entities;

namespace VirtualWardrobe.IntegrationTests.Wishlist;

public sealed class WishlistItemTests
{
    [Fact]
    public async Task WishlistCrudWithHistoryFilteringAndOwnerIsolationShouldWork()
    {
        await using var dbContext = CreateDbContext();

        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var ownerMediaId = Guid.NewGuid();

        dbContext.MediaAssets.Add(new MediaAssetRecord
        {
            Id = ownerMediaId,
            UserId = ownerId,
            StorageKey = "users/a/media/1/inspiration.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 1024,
            Visibility = "PrivateOwnerOnly",
            CreatedAtUtc = DateTime.UtcNow
        });

        var purchasedItemId = Guid.NewGuid();
        dbContext.WishlistItems.Add(new WishlistItemRecord
        {
            Id = purchasedItemId,
            UserId = ownerId,
            Category = ClothingCategory.Shirt.ToString(),
            Name = "Camisa histórica",
            TargetPrice = 120m,
            Status = WishlistItemStatus.Purchased.ToString(),
            PurchasedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var command = CreateCommand(dbContext);

        var createResult = await command.CreateAsync(
            new CreateWishlistItemInput(
                ownerId,
                ClothingCategory.Shoes,
                "Tênis",
                "Marca A",
                350m,
                ownerMediaId,
                ["https://shop.example.com/items/1"]),
            CancellationToken.None);

        Assert.True(createResult.IsSuccess);

        var updateResult = await command.UpdateAsync(
            new UpdateWishlistItemInput(
                createResult.Value.Id.Value,
                ownerId,
                ClothingCategory.Shoes,
                "Tênis atualizado",
                "Marca B",
                370m,
                ownerMediaId,
                ["https://shop.example.com/items/2"]),
            CancellationToken.None);

        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Tênis atualizado", updateResult.Value.Name);

        var activeList = await command.ListAsync(ownerId, false, CancellationToken.None);
        Assert.Single(activeList);
        Assert.Equal(WishlistItemStatus.Active, activeList[0].Status);

        var includePurchasedList = await command.ListAsync(ownerId, true, CancellationToken.None);
        Assert.Equal(2, includePurchasedList.Count);

        var otherOwnerList = await command.ListAsync(otherOwnerId, true, CancellationToken.None);
        Assert.Empty(otherOwnerList);

        var deleteResult = await command.DeleteAsync(createResult.Value.Id.Value, ownerId, CancellationToken.None);
        Assert.True(deleteResult.IsSuccess);

        var listAfterDelete = await command.ListAsync(ownerId, true, CancellationToken.None);
        Assert.Single(listAfterDelete);
        Assert.Equal(purchasedItemId, listAfterDelete[0].Id.Value);
    }

    [Fact]
    public async Task CreateWishlistItemWithDuplicateLinksShouldFailValidation()
    {
        await using var dbContext = CreateDbContext();
        var command = CreateCommand(dbContext);

        var result = await command.CreateAsync(
            new CreateWishlistItemInput(
                Guid.NewGuid(),
                ClothingCategory.Shirt,
                "Camisa",
                null,
                80m,
                null,
                ["https://a.com/p/1", "https://a.com/p/1"]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error.Code);
    }

    private static CreateWishlistItemCommand CreateCommand(VirtualWardrobeDbContext dbContext)
    {
        var wishlistRepository = new EfWishlistItemRepository(dbContext);
        var mediaRepository = new EfMediaAssetRepository(dbContext);
        return new CreateWishlistItemCommand(wishlistRepository, mediaRepository);
    }

    private static VirtualWardrobeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VirtualWardrobeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new VirtualWardrobeDbContext(options);
    }
}
