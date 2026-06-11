namespace VirtualWardrobe.Infrastructure.Persistence.Entities;

public sealed class WishlistExternalLinkRecord
{
    public Guid Id { get; set; }

    public Guid WishlistItemId { get; set; }

    public WishlistItemRecord WishlistItem { get; set; } = default!;

    public string Url { get; set; } = string.Empty;

    public string? Label { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
