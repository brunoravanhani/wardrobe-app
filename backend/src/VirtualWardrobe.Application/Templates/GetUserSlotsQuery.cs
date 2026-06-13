using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Templates;

namespace VirtualWardrobe.Application.Templates;

public sealed class GetUserSlotsQuery
{
    private readonly ITemplateSlotRepository _slotRepository;
    private readonly IUserActiveTemplateRepository _userActiveTemplateRepository;

    public GetUserSlotsQuery(
        ITemplateSlotRepository slotRepository,
        IUserActiveTemplateRepository userActiveTemplateRepository)
    {
        _slotRepository = slotRepository;
        _userActiveTemplateRepository = userActiveTemplateRepository;
    }

    public async Task<(Guid? ActiveTemplateId, IReadOnlyList<TemplateSlot> Slots)> ExecuteAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(ownerUserId);
        var activeTemplateId = await _userActiveTemplateRepository.GetActiveTemplateIdAsync(userId, cancellationToken);

        if (!activeTemplateId.HasValue)
        {
            return (null, Array.Empty<TemplateSlot>());
        }

        var slots = await _slotRepository.ListByUserAndTemplateAsync(
            userId,
            new WardrobeTemplateId(activeTemplateId.Value),
            cancellationToken);

        return (activeTemplateId, slots);
    }
}
