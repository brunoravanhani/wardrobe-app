namespace VirtualWardrobe.Infrastructure.Persistence.Entities;

public sealed class WishlistItemRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public UserRecord User { get; set; } = default!;

    public string Category { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public decimal TargetPrice { get; set; }

    public Guid? InspirationImageAssetId { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime? PurchasedAtUtc { get; set; }

    public Guid? ConvertedWardrobeItemId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<WishlistExternalLinkRecord> ExternalLinks { get; set; } = [];
}
