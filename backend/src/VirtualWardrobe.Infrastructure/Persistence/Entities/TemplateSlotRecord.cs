namespace VirtualWardrobe.Infrastructure.Persistence.Entities;

public sealed class TemplateSlotRecord
{
    public Guid Id { get; set; }

    public Guid TemplateId { get; set; }

    public WardrobeTemplateRecord Template { get; set; } = null!;

    public Guid UserId { get; set; }

    public UserRecord User { get; set; } = null!;

    public string Category { get; set; } = string.Empty;

    public Guid? WardrobeItemId { get; set; }

    public Guid? WishlistItemId { get; set; }

    public DateTime? FulfilledAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
