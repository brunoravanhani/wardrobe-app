namespace VirtualWardrobe.Domain.Common;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id)
    {
        Id = id;
    }

    public TId Id { get; protected set; }

    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; protected set; } = DateTime.UtcNow;

    protected void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool Equals(Entity<TId>? other)
    {
        return other is not null && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }
}

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