using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wishlist;

namespace VirtualWardrobe.UnitTests.Wishlist;

public sealed class WishlistValidationTests
{
    [Fact]
    public void CreateWishlistItemWithValidDataShouldSucceed()
    {
        var item = WishlistItem.Create(
            WishlistItemId.New(),
            UserId.New(),
            ClothingCategory.Shoes,
            "Tênis branco",
            "Marca X",
            399.90m,
            null,
            ["https://loja.exemplo.com/tenis"]);

        Assert.Equal(ClothingCategory.Shoes, item.Category);
        Assert.Equal(399.90m, item.TargetPrice);
        Assert.Single(item.ExternalLinks);
    }

    [Fact]
    public void CreateWishlistItemWithNegativeTargetPriceShouldThrow()
    {
        var action = () => WishlistItem.Create(
            WishlistItemId.New(),
            UserId.New(),
            ClothingCategory.Coats,
            "Casaco",
            null,
            -1m,
            null,
            []);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Contains("Target price", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateWishlistItemWithInvalidExternalLinkShouldThrow()
    {
        var action = () => WishlistItem.Create(
            WishlistItemId.New(),
            UserId.New(),
            ClothingCategory.Shirt,
            "Camisa",
            null,
            100m,
            null,
            ["notaurl"]);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void CreateWishlistItemWithDuplicateExternalLinksShouldThrow()
    {
        var action = () => WishlistItem.Create(
            WishlistItemId.New(),
            UserId.New(),
            ClothingCategory.Shirt,
            "Camisa",
            null,
            100m,
            null,
            ["https://a.com/p/1", "https://a.com/p/1"]);

        Assert.Throws<ArgumentException>(action);
    }
}
