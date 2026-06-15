using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Templates;

namespace VirtualWardrobe.Application.Templates;

public sealed record SelectTemplateInput(Guid UserId, Guid TemplateId);

public sealed class SelectTemplateCommand
{
    private readonly IWardrobeTemplateRepository _templateRepository;
    private readonly ITemplateSlotRepository _slotRepository;
    private readonly IUserActiveTemplateRepository _userActiveTemplateRepository;
    private readonly IWardrobeItemRepository _wardrobeItemRepository;
    private readonly TemplateSlotFulfillmentService _fulfillmentService;

    public SelectTemplateCommand(
        IWardrobeTemplateRepository templateRepository,
        ITemplateSlotRepository slotRepository,
        IUserActiveTemplateRepository userActiveTemplateRepository,
        IWardrobeItemRepository wardrobeItemRepository,
        TemplateSlotFulfillmentService fulfillmentService)
    {
        _templateRepository = templateRepository;
        _slotRepository = slotRepository;
        _userActiveTemplateRepository = userActiveTemplateRepository;
        _wardrobeItemRepository = wardrobeItemRepository;
        _fulfillmentService = fulfillmentService;
    }

    public async Task<Result> ExecuteAsync(SelectTemplateInput input, CancellationToken cancellationToken)
    {
        var userId = new UserId(input.UserId);
        var templateId = new WardrobeTemplateId(input.TemplateId);

        var template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
        {
            return Result.Failure(ResultError.NotFound("Template was not found."));
        }

        var currentActiveTemplateId = await _userActiveTemplateRepository.GetActiveTemplateIdAsync(userId, cancellationToken);

        if (currentActiveTemplateId.HasValue && currentActiveTemplateId.Value == input.TemplateId)
        {
            return Result.Success();
        }

        if (currentActiveTemplateId.HasValue)
        {
            await _slotRepository.DeleteUnfulfilledByUserAndTemplateAsync(
                userId,
                new WardrobeTemplateId(currentActiveTemplateId.Value),
                cancellationToken);
        }

        var now = DateTime.UtcNow;
        var newSlots = new List<TemplateSlot>();
        foreach (var definition in template.SlotDefinitions)
        {
            for (var i = 0; i < definition.Quantity; i++)
            {
                newSlots.Add(TemplateSlot.Create(
                    TemplateSlotId.New(),
                    templateId,
                    userId,
                    definition.Category,
                    now));
            }
        }

        await _slotRepository.AddRangeAsync(newSlots, cancellationToken);
        await _userActiveTemplateRepository.SetActiveTemplateIdAsync(userId, input.TemplateId, cancellationToken);
        await _slotRepository.SaveChangesAsync(cancellationToken);

        var existingItems = await _wardrobeItemRepository.ListAsync(userId, null, cancellationToken);
        foreach (var item in existingItems.OrderBy(x => x.CreatedAtUtc))
        {
            await _fulfillmentService.TryFulfillAsync(userId, item, cancellationToken);
        }

        await _slotRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
