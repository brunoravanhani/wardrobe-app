using Microsoft.EntityFrameworkCore;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Infrastructure.Persistence;
using VirtualWardrobe.Infrastructure.Persistence.Configurations;
using VirtualWardrobe.Infrastructure.Persistence.Entities;

namespace VirtualWardrobe.IntegrationTests.Wardrobe;

public sealed class WardrobeItemTests
{
    [Fact]
    public async Task WardrobeCrudWithCategoryFilteringAndOwnerIsolationShouldWork()
    {
        await using var dbContext = CreateDbContext();

        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var ownerMediaId = Guid.NewGuid();

        dbContext.MediaAssets.Add(new MediaAssetRecord
        {
            Id = ownerMediaId,
            UserId = ownerId,
            StorageKey = "users/a/media/1/body.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 1024,
            Visibility = "PrivateOwnerOnly",
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var command = CreateCommand(dbContext);

        var createResult = await command.CreateAsync(
            new CreateWardrobeItemInput(
                ownerId,
                ClothingCategory.TShirt,
                "Camiseta",
                "M",
                "Marca A",
                79.90m,
                ownerMediaId,
                null),
            CancellationToken.None);

        Assert.True(createResult.IsSuccess);

        var otherUserList = await command.ListAsync(otherOwnerId, null, CancellationToken.None);
        Assert.Empty(otherUserList);

        var ownerList = await command.ListAsync(ownerId, ClothingCategory.TShirt, CancellationToken.None);
        Assert.Single(ownerList);

        var item = ownerList[0];
        var updateResult = await command.UpdateAsync(
            new UpdateWardrobeItemInput(
                item.Id.Value,
                ownerId,
                ClothingCategory.Shirt,
                "Camisa",
                "G",
                "Marca B",
                99.90m,
                ownerMediaId,
                null),
            CancellationToken.None);

        Assert.True(updateResult.IsSuccess);
        Assert.Equal(ClothingCategory.Shirt, updateResult.Value.Category);

        var deleteResult = await command.DeleteAsync(item.Id.Value, ownerId, CancellationToken.None);
        Assert.True(deleteResult.IsSuccess);

        var afterDelete = await command.ListAsync(ownerId, null, CancellationToken.None);
        Assert.Empty(afterDelete);
    }

    private static CreateWardrobeItemCommand CreateCommand(VirtualWardrobeDbContext dbContext)
    {
        var wardrobeRepository = new EfWardrobeItemRepository(dbContext);
        var mediaRepository = new EfMediaAssetRepository(dbContext);
        return new CreateWardrobeItemCommand(wardrobeRepository, mediaRepository);
    }

    private static VirtualWardrobeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VirtualWardrobeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new VirtualWardrobeDbContext(options);
    }
}
