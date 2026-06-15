using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wardrobe;

namespace VirtualWardrobe.Application.Templates;

public sealed class TemplateSlotFulfillmentService
{
    private readonly ITemplateSlotRepository _slotRepository;

    public TemplateSlotFulfillmentService(ITemplateSlotRepository slotRepository)
    {
        _slotRepository = slotRepository;
    }

    /// <summary>
    /// Finds the oldest open slot for the item's category and assigns it.
    /// Does NOT call SaveChangesAsync — the caller is responsible for persisting.
    /// </summary>
    public async Task TryFulfillAsync(UserId userId, WardrobeItem item, CancellationToken cancellationToken)
    {
        var openSlots = await _slotRepository.ListOpenByUserAndCategoryAsync(userId, item.Category, cancellationToken);
        var oldest = openSlots.OrderBy(s => s.CreatedAtUtc).FirstOrDefault();

        if (oldest is null)
        {
            return;
        }

        oldest.Fulfill(item.Id);
        await _slotRepository.UpdateAsync(oldest, cancellationToken);
    }

    /// <summary>
    /// Finds the slot currently fulfilled by wardrobeItemId and reverts it to open.
    /// Does NOT call SaveChangesAsync — the caller is responsible for persisting.
    /// </summary>
    public async Task TryUnfulfillAsync(WardrobeItemId wardrobeItemId, CancellationToken cancellationToken)
    {
        var slot = await _slotRepository.GetByWardrobeItemIdAsync(wardrobeItemId, cancellationToken);
        if (slot is null)
        {
            return;
        }

        slot.Unfulfill();
        await _slotRepository.UpdateAsync(slot, cancellationToken);
    }
}
