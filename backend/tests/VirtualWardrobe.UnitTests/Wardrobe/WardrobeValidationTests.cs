using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Media;
using VirtualWardrobe.Domain.Wardrobe;

namespace VirtualWardrobe.UnitTests.Wardrobe;

public sealed class WardrobeValidationTests
{
    [Fact]
    public void CreateWardrobeItemWithValidDataShouldSucceed()
    {
        var item = WardrobeItem.Create(
            WardrobeItemId.New(),
            UserId.New(),
            ClothingCategory.TShirt,
            "Camiseta branca",
            "M",
            "Marca X",
            99.90m);

        Assert.Equal(ClothingCategory.TShirt, item.Category);
        Assert.Equal("Camiseta branca", item.Name);
    }

    [Fact]
    public void CreateWardrobeItemWithNegativePriceShouldThrow()
    {
        var action = () => WardrobeItem.Create(
            WardrobeItemId.New(),
            UserId.New(),
            ClothingCategory.Shirt,
            "Camisa",
            "G",
            price: -1m);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Contains("Price", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateMediaAssetWithUnsupportedContentTypeShouldThrow()
    {
        var action = () => MediaAsset.Create(
            MediaAssetId.New(),
            UserId.New(),
            "users/u/media/x/file.gif",
            "image/gif",
            1024);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void CreateMediaAssetWithFileOver10MbShouldThrow()
    {
        var action = () => MediaAsset.Create(
            MediaAssetId.New(),
            UserId.New(),
            "users/u/media/x/file.webp",
            "image/webp",
            10 * 1024 * 1024 + 1);

        Assert.Throws<ArgumentException>(action);
    }
}
