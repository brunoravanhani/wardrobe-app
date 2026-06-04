using VirtualWardrobe.Domain.Common;

namespace VirtualWardrobe.Domain.Wishlist;

public enum WishlistItemStatus
{
    Active = 1,
    Purchased = 2
}

public sealed class WishlistItem : Entity<WishlistItemId>
{
    private readonly List<WishlistExternalLink> _externalLinks;

    private WishlistItem(
        WishlistItemId id,
        UserId ownerUserId,
        ClothingCategory category,
        string name,
        string? brand,
        decimal targetPrice,
        MediaAssetId? inspirationImageAssetId,
        WishlistItemStatus status,
        DateTime? purchasedAtUtc,
        Guid? convertedWardrobeItemId,
        IEnumerable<WishlistExternalLink> externalLinks,
        DateTime createdAtUtc)
        : base(id)
    {
        OwnerUserId = ownerUserId;
        Category = category;
        Name = name;
        Brand = brand;
        TargetPrice = targetPrice;
        InspirationImageAssetId = inspirationImageAssetId;
        Status = status;
        PurchasedAtUtc = purchasedAtUtc;
        ConvertedWardrobeItemId = convertedWardrobeItemId;
        _externalLinks = externalLinks.ToList();
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public UserId OwnerUserId { get; }

    public ClothingCategory Category { get; private set; }

    public string Name { get; private set; }

    public string? Brand { get; private set; }

    public decimal TargetPrice { get; private set; }

    public MediaAssetId? InspirationImageAssetId { get; private set; }

    public WishlistItemStatus Status { get; private set; }

    public DateTime? PurchasedAtUtc { get; private set; }

    public Guid? ConvertedWardrobeItemId { get; private set; }

    public IReadOnlyList<WishlistExternalLink> ExternalLinks => _externalLinks;

    public static WishlistItem Create(
        WishlistItemId id,
        UserId ownerUserId,
        ClothingCategory category,
        string name,
        string? brand,
        decimal targetPrice,
        MediaAssetId? inspirationImageAssetId,
        IEnumerable<string>? externalLinks = null,
        DateTime? createdAtUtc = null)
    {
        Validate(ownerUserId, name, targetPrice);

        var linkEntities = CreateLinks(id, externalLinks, createdAtUtc);

        return new WishlistItem(
            id,
            ownerUserId,
            category,
            name.Trim(),
            NormalizeOptional(brand),
            targetPrice,
            inspirationImageAssetId,
            WishlistItemStatus.Active,
            null,
            null,
            linkEntities,
            createdAtUtc ?? DateTime.UtcNow);
    }

    public static WishlistItem Rehydrate(
        WishlistItemId id,
        UserId ownerUserId,
        ClothingCategory category,
        string name,
        string? brand,
        decimal targetPrice,
        MediaAssetId? inspirationImageAssetId,
        WishlistItemStatus status,
        DateTime? purchasedAtUtc,
        Guid? convertedWardrobeItemId,
        IEnumerable<WishlistExternalLink> externalLinks,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        var item = new WishlistItem(
            id,
            ownerUserId,
            category,
            name.Trim(),
            NormalizeOptional(brand),
            targetPrice,
            inspirationImageAssetId,
            status,
            purchasedAtUtc,
            convertedWardrobeItemId,
            externalLinks,
            createdAtUtc);

        Validate(item.OwnerUserId, item.Name, item.TargetPrice);
        ValidateDuplicateLinks(item._externalLinks.Select(x => x.Url));

        item.UpdatedAtUtc = updatedAtUtc;
        return item;
    }

    public void Update(
        ClothingCategory category,
        string name,
        string? brand,
        decimal targetPrice,
        MediaAssetId? inspirationImageAssetId,
        IEnumerable<string>? externalLinks)
    {
        Validate(OwnerUserId, name, targetPrice);

        Category = category;
        Name = name.Trim();
        Brand = NormalizeOptional(brand);
        TargetPrice = targetPrice;
        InspirationImageAssetId = inspirationImageAssetId;

        _externalLinks.Clear();
        _externalLinks.AddRange(CreateLinks(Id, externalLinks, DateTime.UtcNow));

        Touch();
    }

    public void MarkAsPurchased(DateTime? purchasedAtUtc = null)
    {
        if (Status == WishlistItemStatus.Purchased)
        {
            return;
        }

        Status = WishlistItemStatus.Purchased;
        PurchasedAtUtc = purchasedAtUtc ?? DateTime.UtcNow;
        Touch();
    }

    private static void Validate(UserId ownerUserId, string name, decimal targetPrice)
    {
        if (ownerUserId.Value == Guid.Empty)
        {
            throw new ArgumentException("Owner user is required.", nameof(ownerUserId));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
        {
            throw new ArgumentException("Name must be between 1 and 120 characters.", nameof(name));
        }

        if (targetPrice < 0)
        {
            throw new ArgumentException("Target price cannot be negative.", nameof(targetPrice));
        }
    }

    private static List<WishlistExternalLink> CreateLinks(
        WishlistItemId wishlistItemId,
        IEnumerable<string>? links,
        DateTime? createdAtUtc)
    {
        var sanitized = (links ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();

        ValidateDuplicateLinks(sanitized);

        return sanitized
            .Select(link => WishlistExternalLink.Create(
                WishlistExternalLinkId.New(),
                wishlistItemId,
                link,
                createdAtUtc ?? DateTime.UtcNow))
            .ToList();
    }

    private static void ValidateDuplicateLinks(IEnumerable<string> links)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var link in links)
        {
            if (!set.Add(link))
            {
                throw new ArgumentException("Duplicate external links are not allowed.", nameof(links));
            }
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > 120 ? trimmed[..120] : trimmed;
    }
}
