namespace VirtualWardrobe.Infrastructure.Persistence.Entities;

public sealed class WardrobeTemplateRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<TemplateSlotDefinitionRecord> SlotDefinitions { get; set; } = [];

    public List<TemplateSlotRecord> TemplateSlots { get; set; } = [];
}
