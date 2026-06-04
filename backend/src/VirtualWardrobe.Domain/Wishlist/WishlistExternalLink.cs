using VirtualWardrobe.Domain.Common;

namespace VirtualWardrobe.Domain.Wishlist;

public sealed class WishlistExternalLink : Entity<WishlistExternalLinkId>
{
    private WishlistExternalLink(
        WishlistExternalLinkId id,
        WishlistItemId wishlistItemId,
        string url,
        DateTime createdAtUtc)
        : base(id)
    {
        WishlistItemId = wishlistItemId;
        Url = url;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public WishlistItemId WishlistItemId { get; }

    public string Url { get; }

    public static WishlistExternalLink Create(
        WishlistExternalLinkId id,
        WishlistItemId wishlistItemId,
        string url,
        DateTime? createdAtUtc = null)
    {
        if (wishlistItemId.Value == Guid.Empty)
        {
            throw new ArgumentException("Wishlist item is required.", nameof(wishlistItemId));
        }

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out _))
        {
            throw new ArgumentException("External link URL must be a valid absolute URL.", nameof(url));
        }

        return new WishlistExternalLink(id, wishlistItemId, url.Trim(), createdAtUtc ?? DateTime.UtcNow);
    }

    public static WishlistExternalLink Rehydrate(
        WishlistExternalLinkId id,
        WishlistItemId wishlistItemId,
        string url,
        DateTime createdAtUtc)
    {
        return Create(id, wishlistItemId, url, createdAtUtc);
    }
}
