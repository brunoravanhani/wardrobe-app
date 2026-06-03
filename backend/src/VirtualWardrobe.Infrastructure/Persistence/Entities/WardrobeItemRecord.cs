namespace VirtualWardrobe.Infrastructure.Persistence.Entities;

public sealed class WardrobeItemRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public UserRecord User { get; set; } = default!;

    public string Category { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string Size { get; set; } = string.Empty;

    public decimal? Price { get; set; }

    public Guid? BodyImageAssetId { get; set; }

    public Guid? CareTagImageAssetId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
