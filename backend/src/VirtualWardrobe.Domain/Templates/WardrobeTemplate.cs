using VirtualWardrobe.Domain.Common;

namespace VirtualWardrobe.Domain.Templates;

public sealed record TemplateSlotDefinition(
    TemplateSlotDefinitionId Id,
    WardrobeTemplateId TemplateId,
    ClothingCategory Category,
    int Quantity);

public sealed class WardrobeTemplate
{
    public WardrobeTemplate(WardrobeTemplateId id, string name, IReadOnlyList<TemplateSlotDefinition> slotDefinitions)
    {
        Id = id;
        Name = name;
        SlotDefinitions = slotDefinitions;
    }

    public WardrobeTemplateId Id { get; }
    public string Name { get; }
    public IReadOnlyList<TemplateSlotDefinition> SlotDefinitions { get; }

    public int TotalSlots => SlotDefinitions.Sum(d => d.Quantity);
}
