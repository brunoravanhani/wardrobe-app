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
            [("https://loja.exemplo.com/tenis", "Ver na Loja")]);

        Assert.Equal(ClothingCategory.Shoes, item.Category);
        Assert.Equal(399.90m, item.TargetPrice);
        Assert.Single(item.ExternalLinks);
        Assert.Equal("Ver na Loja", item.ExternalLinks[0].Label);
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
            [("notaurl", null)]);

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
            [("https://a.com/p/1", null), ("https://a.com/p/1", null)]);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void WishlistLinkLabelShouldBeTrimmed()
    {
        var link = WishlistExternalLink.Create(
            WishlistExternalLinkId.New(),
            WishlistItemId.New(),
            "https://loja.exemplo/item",
            "  Ver na Loja  ");

        Assert.Equal("Ver na Loja", link.Label);
    }

    [Fact]
    public void WishlistLinkEmptyLabelShouldBeNull()
    {
        var link = WishlistExternalLink.Create(
            WishlistExternalLinkId.New(),
            WishlistItemId.New(),
            "https://loja.exemplo/item",
            "   ");

        Assert.Null(link.Label);
    }

    [Fact]
    public void WishlistLinkLabelOver80CharsShouldThrow()
    {
        var longLabel = new string('a', 81);
        var action = () => WishlistExternalLink.Create(
            WishlistExternalLinkId.New(),
            WishlistItemId.New(),
            "https://loja.exemplo/item",
            longLabel);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void WishlistLinkLabelNullShouldBeAccepted()
    {
        var link = WishlistExternalLink.Create(
            WishlistExternalLinkId.New(),
            WishlistItemId.New(),
            "https://loja.exemplo/item",
            null);

        Assert.Null(link.Label);
    }

    [Fact]
    public void DuplicateUrlWithDifferentLabelsShouldThrow()
    {
        var action = () => WishlistItem.Create(
            WishlistItemId.New(),
            UserId.New(),
            ClothingCategory.Shirt,
            "Camisa",
            null,
            100m,
            null,
            [("https://a.com/p/1", "Label A"), ("https://a.com/p/1", "Label B")]);

        Assert.Throws<ArgumentException>(action);
    }
}
