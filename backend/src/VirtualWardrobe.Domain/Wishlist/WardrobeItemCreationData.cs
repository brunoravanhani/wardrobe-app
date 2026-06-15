using VirtualWardrobe.Domain.Common;

namespace VirtualWardrobe.Domain.Wishlist;

public sealed record WardrobeItemCreationData(
    ClothingCategory Category,
    string Name,
    string? Brand,
    decimal TargetPrice
);
