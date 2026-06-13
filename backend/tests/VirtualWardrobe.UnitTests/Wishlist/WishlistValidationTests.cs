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

    [Fact]
    public void ConvertToWardrobeOnActiveItemSetsStatusToPurchasedAndReturnsCreationData()
    {
        var item = WishlistItem.Create(
            WishlistItemId.New(),
            UserId.New(),
            ClothingCategory.Shirt,
            "Camisa casual",
            "Marca E",
            150m,
            null);

        var data = item.ConvertToWardrobe();

        Assert.Equal(WishlistItemStatus.Purchased, item.Status);
        Assert.NotNull(item.PurchasedAtUtc);
        Assert.Equal(ClothingCategory.Shirt, data.Category);
        Assert.Equal("Camisa casual", data.Name);
        Assert.Equal("Marca E", data.Brand);
        Assert.Equal(150m, data.TargetPrice);
    }

    [Fact]
    public void ConvertToWardrobeOnPurchasedItemThrowsArgumentException()
    {
        var item = WishlistItem.Create(
            WishlistItemId.New(),
            UserId.New(),
            ClothingCategory.Pants,
            "Calça jeans",
            null,
            200m,
            null);
        item.MarkAsPurchased();

        var action = () => item.ConvertToWardrobe();

        Assert.Throws<ArgumentException>(action);
    }
}
