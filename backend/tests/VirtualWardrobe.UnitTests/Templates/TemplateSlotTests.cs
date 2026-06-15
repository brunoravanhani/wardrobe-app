using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Templates;

namespace VirtualWardrobe.UnitTests.Templates;

public sealed class TemplateSlotTests
{
    [Fact]
    public void FulfillOpenSlotShouldAssignWardrobeItemAndClearWishlistLink()
    {
        var slot = TemplateSlot.Create(
            TemplateSlotId.New(),
            new WardrobeTemplateId(Guid.NewGuid()),
            new UserId(Guid.NewGuid()),
            ClothingCategory.TShirt);

        var wishlistItemId = WishlistItemId.New();
        slot.LinkToWishlist(wishlistItemId);

        var wardrobeItemId = WardrobeItemId.New();
        slot.Fulfill(wardrobeItemId);

        Assert.True(slot.IsFulfilled);
        Assert.Equal(wardrobeItemId, slot.WardrobeItemId);
        Assert.NotNull(slot.FulfilledAtUtc);
        Assert.Null(slot.WishlistItemId);
    }

    [Fact]
    public void FulfillAlreadyFulfilledSlotShouldThrow()
    {
        var slot = TemplateSlot.Create(
            TemplateSlotId.New(),
            new WardrobeTemplateId(Guid.NewGuid()),
            new UserId(Guid.NewGuid()),
            ClothingCategory.Shirt);

        slot.Fulfill(WardrobeItemId.New());

        Assert.Throws<InvalidOperationException>(() => slot.Fulfill(WardrobeItemId.New()));
    }

    [Fact]
    public void UnfulfillFulfilledSlotShouldClearWardrobeItem()
    {
        var slot = TemplateSlot.Create(
            TemplateSlotId.New(),
            new WardrobeTemplateId(Guid.NewGuid()),
            new UserId(Guid.NewGuid()),
            ClothingCategory.Pants);

        slot.Fulfill(WardrobeItemId.New());
        slot.Unfulfill();

        Assert.False(slot.IsFulfilled);
        Assert.Null(slot.WardrobeItemId);
        Assert.Null(slot.FulfilledAtUtc);
    }

    [Fact]
    public void UnfulfillOpenSlotShouldBeNoOp()
    {
        var slot = TemplateSlot.Create(
            TemplateSlotId.New(),
            new WardrobeTemplateId(Guid.NewGuid()),
            new UserId(Guid.NewGuid()),
            ClothingCategory.Shoes);

        slot.Unfulfill();

        Assert.False(slot.IsFulfilled);
    }

    [Fact]
    public void LinkToWishlistOnOpenSlotShouldSetWishlistItemId()
    {
        var slot = TemplateSlot.Create(
            TemplateSlotId.New(),
            new WardrobeTemplateId(Guid.NewGuid()),
            new UserId(Guid.NewGuid()),
            ClothingCategory.Coats);

        var wishlistItemId = WishlistItemId.New();
        slot.LinkToWishlist(wishlistItemId);

        Assert.Equal(wishlistItemId, slot.WishlistItemId);
    }

    [Fact]
    public void LinkToWishlistOnFulfilledSlotShouldThrow()
    {
        var slot = TemplateSlot.Create(
            TemplateSlotId.New(),
            new WardrobeTemplateId(Guid.NewGuid()),
            new UserId(Guid.NewGuid()),
            ClothingCategory.TShirt);

        slot.Fulfill(WardrobeItemId.New());

        Assert.Throws<InvalidOperationException>(() => slot.LinkToWishlist(WishlistItemId.New()));
    }
}
