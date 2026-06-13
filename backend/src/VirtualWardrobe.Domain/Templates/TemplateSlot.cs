using VirtualWardrobe.Domain.Common;

namespace VirtualWardrobe.Domain.Templates;

public sealed class TemplateSlot : Entity<TemplateSlotId>
{
    private TemplateSlot(
        TemplateSlotId id,
        WardrobeTemplateId templateId,
        UserId ownerUserId,
        ClothingCategory category,
        WardrobeItemId? wardrobeItemId,
        WishlistItemId? wishlistItemId,
        DateTime? fulfilledAtUtc,
        DateTime createdAtUtc)
        : base(id)
    {
        TemplateId = templateId;
        OwnerUserId = ownerUserId;
        Category = category;
        WardrobeItemId = wardrobeItemId;
        WishlistItemId = wishlistItemId;
        FulfilledAtUtc = fulfilledAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public WardrobeTemplateId TemplateId { get; }
    public UserId OwnerUserId { get; }
    public ClothingCategory Category { get; }
    public WardrobeItemId? WardrobeItemId { get; private set; }
    public WishlistItemId? WishlistItemId { get; private set; }
    public DateTime? FulfilledAtUtc { get; private set; }
    public bool IsFulfilled => WardrobeItemId.HasValue;

    public static TemplateSlot Create(
        TemplateSlotId id,
        WardrobeTemplateId templateId,
        UserId ownerUserId,
        ClothingCategory category,
        DateTime? createdAtUtc = null)
    {
        return new TemplateSlot(id, templateId, ownerUserId, category, null, null, null, createdAtUtc ?? DateTime.UtcNow);
    }

    public static TemplateSlot Rehydrate(
        TemplateSlotId id,
        WardrobeTemplateId templateId,
        UserId ownerUserId,
        ClothingCategory category,
        WardrobeItemId? wardrobeItemId,
        WishlistItemId? wishlistItemId,
        DateTime? fulfilledAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        var slot = new TemplateSlot(id, templateId, ownerUserId, category, wardrobeItemId, wishlistItemId, fulfilledAtUtc, createdAtUtc);
        slot.UpdatedAtUtc = updatedAtUtc;
        return slot;
    }

    public void Fulfill(WardrobeItemId wardrobeItemId, DateTime? fulfilledAtUtc = null)
    {
        if (IsFulfilled)
        {
            throw new InvalidOperationException("Slot is already fulfilled.");
        }

        WardrobeItemId = wardrobeItemId;
        WishlistItemId = null;
        FulfilledAtUtc = fulfilledAtUtc ?? DateTime.UtcNow;
        Touch();
    }

    public void Unfulfill()
    {
        if (!IsFulfilled)
        {
            return;
        }

        WardrobeItemId = null;
        FulfilledAtUtc = null;
        Touch();
    }

    public void LinkToWishlist(WishlistItemId wishlistItemId)
    {
        if (IsFulfilled)
        {
            throw new InvalidOperationException("Cannot link a fulfilled slot to a wishlist item.");
        }

        WishlistItemId = wishlistItemId;
        Touch();
    }
}
