using Microsoft.EntityFrameworkCore;
using VirtualWardrobe.Application.Wishlist;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wishlist;
using VirtualWardrobe.Infrastructure.Persistence;
using VirtualWardrobe.Infrastructure.Persistence.Configurations;
using VirtualWardrobe.Infrastructure.Persistence.Entities;

namespace VirtualWardrobe.IntegrationTests.Wishlist;

public sealed class WishlistConversionTests
{
    [Fact]
    public async Task PurchaseAndConvertShouldPersistHistoryAndBeIdempotent()
    {
        await using var dbContext = CreateDbContext();

        var ownerId = Guid.NewGuid();
        var wishlistItemId = Guid.NewGuid();

        dbContext.WishlistItems.Add(new WishlistItemRecord
        {
            Id = wishlistItemId,
            UserId = ownerId,
            Category = ClothingCategory.Shirt.ToString(),
            Name = "Camisa social",
            Brand = "Marca C",
            TargetPrice = 180m,
            Status = WishlistItemStatus.Active.ToString(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var command = CreateCommand(dbContext);

        var purchaseResult = await command.MarkAsPurchasedAsync(wishlistItemId, ownerId, CancellationToken.None);
        Assert.True(purchaseResult.IsSuccess);

        var firstConvert = await command.ConvertToWardrobeAsync(
            new ConvertWishlistItemInput(
                wishlistItemId,
                ownerId,
                null,
                null,
                "M",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        var secondConvert = await command.ConvertToWardrobeAsync(
            new ConvertWishlistItemInput(
                wishlistItemId,
                ownerId,
                null,
                null,
                "M",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.True(firstConvert.IsSuccess);
        Assert.True(secondConvert.IsSuccess);
        Assert.Equal(firstConvert.Value.Id, secondConvert.Value.Id);

        var activeItems = await command.ListAsync(ownerId, false, CancellationToken.None);
        Assert.Empty(activeItems);

        var historyItems = await command.ListAsync(ownerId, true, CancellationToken.None);
        Assert.Single(historyItems);
        Assert.Equal(WishlistItemStatus.Purchased, historyItems[0].Status);
        Assert.Equal(firstConvert.Value.Id.Value, historyItems[0].ConvertedWardrobeItemId);

        var wardrobeCount = await dbContext.WardrobeItems.CountAsync(x => x.UserId == ownerId);
        Assert.Equal(1, wardrobeCount);
    }

    private static ConvertWishlistItemCommand CreateCommand(VirtualWardrobeDbContext dbContext)
    {
        var wishlistRepository = new EfWishlistItemRepository(dbContext);
        var wardrobeRepository = new EfWardrobeItemRepository(dbContext);
        var mediaRepository = new EfMediaAssetRepository(dbContext);
        return new ConvertWishlistItemCommand(wishlistRepository, wardrobeRepository, mediaRepository);
    }

    private static VirtualWardrobeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VirtualWardrobeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new VirtualWardrobeDbContext(options);
    }
}