namespace VirtualWardrobe.Domain.Common;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct MediaAssetId(Guid Value)
{
    public static MediaAssetId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct WardrobeItemId(Guid Value)
{
    public static WardrobeItemId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct WishlistItemId(Guid Value)
{
    public static WishlistItemId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct WishlistExternalLinkId(Guid Value)
{
    public static WishlistExternalLinkId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct WardrobeTemplateId(Guid Value)
{
    public static WardrobeTemplateId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct TemplateSlotDefinitionId(Guid Value)
{
    public static TemplateSlotDefinitionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct TemplateSlotId(Guid Value)
{
    public static TemplateSlotId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
