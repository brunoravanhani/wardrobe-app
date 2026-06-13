using Microsoft.EntityFrameworkCore;
using VirtualWardrobe.Application.Templates;
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
    public async Task CombinedConvertActiveItemShouldMarkAsPurchasedAndCreateWardrobeItemInOneCall()
    {
        await using var dbContext = CreateDbContext();

        var ownerId = Guid.NewGuid();
        var wishlistItemId = Guid.NewGuid();

        dbContext.WishlistItems.Add(new WishlistItemRecord
        {
            Id = wishlistItemId,
            UserId = ownerId,
            Category = ClothingCategory.TShirt.ToString(),
            Name = "Camiseta básica",
            Brand = "Marca F",
            TargetPrice = 89.90m,
            Status = WishlistItemStatus.Active.ToString(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var command = CreateCommand(dbContext);

        var result = await command.CombinedConvertAsync(
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

        Assert.True(result.IsSuccess);

        var wardrobeCount = await dbContext.WardrobeItems.CountAsync(x => x.UserId == ownerId);
        Assert.Equal(1, wardrobeCount);

        var wishlistRecord = await dbContext.WishlistItems.FindAsync(wishlistItemId);
        Assert.NotNull(wishlistRecord);
        Assert.Equal(WishlistItemStatus.Purchased.ToString(), wishlistRecord!.Status);
        Assert.NotNull(wishlistRecord.PurchasedAtUtc);
        Assert.Equal(result.Value.Id.Value, wishlistRecord.ConvertedWardrobeItemId);
    }

    [Fact]
    public async Task ConvertShouldPersistHistoryAndBeIdempotent()
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

        var firstConvert = await command.CombinedConvertAsync(
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

        var secondConvert = await command.CombinedConvertAsync(
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
        var slotRepository = new EfTemplateSlotRepository(dbContext);
        var fulfillmentService = new TemplateSlotFulfillmentService(slotRepository);
        return new ConvertWishlistItemCommand(wishlistRepository, wardrobeRepository, mediaRepository, fulfillmentService);
    }

    private static VirtualWardrobeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VirtualWardrobeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new VirtualWardrobeDbContext(options);
    }
}