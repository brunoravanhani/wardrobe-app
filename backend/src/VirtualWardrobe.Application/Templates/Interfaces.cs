using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Templates;

namespace VirtualWardrobe.Application.Templates;

public interface IWardrobeTemplateRepository
{
    Task<IReadOnlyList<WardrobeTemplate>> GetAllAsync(CancellationToken cancellationToken);

    Task<WardrobeTemplate?> GetByIdAsync(WardrobeTemplateId templateId, CancellationToken cancellationToken);
}

public interface ITemplateSlotRepository
{
    Task AddRangeAsync(IEnumerable<TemplateSlot> slots, CancellationToken cancellationToken);

    Task UpdateAsync(TemplateSlot slot, CancellationToken cancellationToken);

    Task<TemplateSlot?> GetByIdAsync(TemplateSlotId slotId, UserId ownerUserId, CancellationToken cancellationToken);

    Task<TemplateSlot?> GetByWardrobeItemIdAsync(WardrobeItemId wardrobeItemId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TemplateSlot>> ListByUserAndTemplateAsync(UserId userId, WardrobeTemplateId templateId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TemplateSlot>> ListOpenByUserAndCategoryAsync(UserId userId, ClothingCategory category, CancellationToken cancellationToken);

    Task DeleteUnfulfilledByUserAndTemplateAsync(UserId userId, WardrobeTemplateId templateId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IUserActiveTemplateRepository
{
    Task<Guid?> GetActiveTemplateIdAsync(UserId userId, CancellationToken cancellationToken);

    Task SetActiveTemplateIdAsync(UserId userId, Guid? templateId, CancellationToken cancellationToken);
}
