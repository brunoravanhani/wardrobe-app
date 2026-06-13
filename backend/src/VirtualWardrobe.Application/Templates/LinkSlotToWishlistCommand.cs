using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Application.Wishlist;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Templates;
using VirtualWardrobe.Domain.Wishlist;

namespace VirtualWardrobe.Application.Templates;

public sealed record LinkSlotToWishlistInput(
    Guid SlotId,
    Guid OwnerUserId,
    string Name,
    string? Brand,
    decimal TargetPrice);

public sealed class LinkSlotToWishlistCommand
{
    private readonly ITemplateSlotRepository _slotRepository;
    private readonly IWishlistItemRepository _wishlistItemRepository;

    public LinkSlotToWishlistCommand(
        ITemplateSlotRepository slotRepository,
        IWishlistItemRepository wishlistItemRepository)
    {
        _slotRepository = slotRepository;
        _wishlistItemRepository = wishlistItemRepository;
    }

    public async Task<Result<WishlistItem>> ExecuteAsync(LinkSlotToWishlistInput input, CancellationToken cancellationToken)
    {
        var userId = new UserId(input.OwnerUserId);
        var slotId = new TemplateSlotId(input.SlotId);

        var slot = await _slotRepository.GetByIdAsync(slotId, userId, cancellationToken);
        if (slot is null)
        {
            return Result.Failure<WishlistItem>(ResultError.NotFound("Slot was not found."));
        }

        if (slot.IsFulfilled)
        {
            return Result.Failure<WishlistItem>(ResultError.Validation("Cannot link a fulfilled slot to a wishlist item."));
        }

        try
        {
            var wishlistItem = WishlistItem.Create(
                WishlistItemId.New(),
                userId,
                slot.Category,
                input.Name,
                input.Brand,
                input.TargetPrice,
                null);

            await _wishlistItemRepository.AddAsync(wishlistItem, cancellationToken);
            slot.LinkToWishlist(wishlistItem.Id);
            await _slotRepository.UpdateAsync(slot, cancellationToken);
            await _wishlistItemRepository.SaveChangesAsync(cancellationToken);

            return Result.Success(wishlistItem);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<WishlistItem>(ResultError.Validation(exception.Message));
        }
    }
}
