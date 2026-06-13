namespace VirtualWardrobe.Infrastructure.Persistence.Entities;

public sealed class TemplateSlotDefinitionRecord
{
    public Guid Id { get; set; }

    public Guid TemplateId { get; set; }

    public WardrobeTemplateRecord Template { get; set; } = null!;

    public string Category { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
