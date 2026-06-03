using VirtualWardrobe.Domain.Common;

namespace VirtualWardrobe.Domain.Wardrobe;

public sealed class WardrobeItem : Entity<WardrobeItemId>
{
    private WardrobeItem(
        WardrobeItemId id,
        UserId ownerUserId,
        ClothingCategory category,
        string name,
        string? brand,
        string size,
        decimal? price,
        MediaAssetId? bodyImageAssetId,
        MediaAssetId? careTagImageAssetId,
        DateTime createdAtUtc)
        : base(id)
    {
        OwnerUserId = ownerUserId;
        Category = category;
        Name = name;
        Brand = brand;
        Size = size;
        Price = price;
        BodyImageAssetId = bodyImageAssetId;
        CareTagImageAssetId = careTagImageAssetId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public UserId OwnerUserId { get; }

    public ClothingCategory Category { get; private set; }

    public string Name { get; private set; }

    public string? Brand { get; private set; }

    public string Size { get; private set; }

    public decimal? Price { get; private set; }

    public MediaAssetId? BodyImageAssetId { get; private set; }

    public MediaAssetId? CareTagImageAssetId { get; private set; }

    public static WardrobeItem Create(
        WardrobeItemId id,
        UserId ownerUserId,
        ClothingCategory category,
        string name,
        string size,
        string? brand = null,
        decimal? price = null,
        MediaAssetId? bodyImageAssetId = null,
        MediaAssetId? careTagImageAssetId = null,
        DateTime? createdAtUtc = null)
    {
        Validate(ownerUserId, name, size, price);

        return new WardrobeItem(
            id,
            ownerUserId,
            category,
            name.Trim(),
            NormalizeOptional(brand),
            size.Trim(),
            price,
            bodyImageAssetId,
            careTagImageAssetId,
            createdAtUtc ?? DateTime.UtcNow);
    }

    public static WardrobeItem Rehydrate(
        WardrobeItemId id,
        UserId ownerUserId,
        ClothingCategory category,
        string name,
        string size,
        string? brand,
        decimal? price,
        MediaAssetId? bodyImageAssetId,
        MediaAssetId? careTagImageAssetId,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        var item = Create(
            id,
            ownerUserId,
            category,
            name,
            size,
            brand,
            price,
            bodyImageAssetId,
            careTagImageAssetId,
            createdAtUtc);

        item.UpdatedAtUtc = updatedAtUtc;
        return item;
    }

    public void Update(
        ClothingCategory category,
        string name,
        string size,
        string? brand,
        decimal? price,
        MediaAssetId? bodyImageAssetId,
        MediaAssetId? careTagImageAssetId)
    {
        Validate(OwnerUserId, name, size, price);

        Category = category;
        Name = name.Trim();
        Brand = NormalizeOptional(brand);
        Size = size.Trim();
        Price = price;
        BodyImageAssetId = bodyImageAssetId;
        CareTagImageAssetId = careTagImageAssetId;
        Touch();
    }

    private static void Validate(UserId ownerUserId, string name, string size, decimal? price)
    {
        if (ownerUserId.Value == Guid.Empty)
        {
            throw new ArgumentException("Owner user is required.", nameof(ownerUserId));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
        {
            throw new ArgumentException("Name must be between 1 and 120 characters.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(size) || size.Trim().Length > 32)
        {
            throw new ArgumentException("Size must be between 1 and 32 characters.", nameof(size));
        }

        if (price is < 0)
        {
            throw new ArgumentException("Price cannot be negative.", nameof(price));
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
