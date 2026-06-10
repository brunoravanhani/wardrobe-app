using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualWardrobe.Application.Wishlist;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wishlist;
using VirtualWardrobe.Infrastructure.Persistence.Entities;

namespace VirtualWardrobe.Infrastructure.Persistence.Configurations;

public sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItemRecord>
{
    public void Configure(EntityTypeBuilder<WishlistItemRecord> builder)
    {
        builder.ToTable("wishlist_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Brand).HasColumnName("brand").HasMaxLength(120);
        builder.Property(x => x.TargetPrice).HasColumnName("target_price").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(x => x.InspirationImageAssetId).HasColumnName("inspiration_image_asset_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(x => x.PurchasedAtUtc).HasColumnName("purchased_at_utc");
        builder.Property(x => x.ConvertedWardrobeItemId).HasColumnName("converted_wardrobe_item_id");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ExternalLinks)
            .WithOne(x => x.WishlistItem)
            .HasForeignKey(x => x.WishlistItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
    }
}

public sealed class WishlistExternalLinkConfiguration : IEntityTypeConfiguration<WishlistExternalLinkRecord>
{
    public void Configure(EntityTypeBuilder<WishlistExternalLinkRecord> builder)
    {
        builder.ToTable("wishlist_external_links");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WishlistItemId).HasColumnName("wishlist_item_id").IsRequired();
        builder.Property(x => x.Url).HasColumnName("url").HasMaxLength(2048).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasOne(x => x.WishlistItem)
            .WithMany(x => x.ExternalLinks)
            .HasForeignKey(x => x.WishlistItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.WishlistItemId);
        builder.HasIndex(x => new { x.WishlistItemId, x.Url }).IsUnique();
    }
}

public sealed class EfWishlistItemRepository : IWishlistItemRepository
{
    private readonly VirtualWardrobeDbContext _dbContext;

    public EfWishlistItemRepository(VirtualWardrobeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WishlistItem item, CancellationToken cancellationToken)
    {
        await _dbContext.WishlistItems.AddAsync(ToRecord(item), cancellationToken);
    }

    public async Task UpdateAsync(WishlistItem item, CancellationToken cancellationToken)
    {
        var record = await _dbContext.WishlistItems
            .Include(x => x.ExternalLinks)
            .SingleOrDefaultAsync(
                x => x.Id == item.Id.Value && x.UserId == item.OwnerUserId.Value,
                cancellationToken);

        if (record is null)
        {
            return;
        }

        record.Category = item.Category.ToString();
        record.Name = item.Name;
        record.Brand = item.Brand;
        record.TargetPrice = item.TargetPrice;
        record.InspirationImageAssetId = item.InspirationImageAssetId?.Value;
        record.Status = item.Status.ToString();
        record.PurchasedAtUtc = item.PurchasedAtUtc;
        record.ConvertedWardrobeItemId = item.ConvertedWardrobeItemId;
        record.CreatedAtUtc = item.CreatedAtUtc;
        record.UpdatedAtUtc = item.UpdatedAtUtc;

        var existingLinks = await _dbContext.WishlistExternalLinks
            .Where(x => x.WishlistItemId == item.Id.Value)
            .ToListAsync(cancellationToken);

        _dbContext.WishlistExternalLinks.RemoveRange(existingLinks);

        await _dbContext.WishlistExternalLinks.AddRangeAsync(
            item.ExternalLinks.Select(link => new WishlistExternalLinkRecord
            {
                Id = link.Id.Value,
                WishlistItemId = item.Id.Value,
                Url = link.Url,
                CreatedAtUtc = link.CreatedAtUtc
            }),
            cancellationToken);
    }

    public async Task<WishlistItem?> GetByIdAsync(WishlistItemId itemId, UserId ownerUserId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.WishlistItems
            .Include(x => x.ExternalLinks)
            .SingleOrDefaultAsync(
                x => x.Id == itemId.Value && x.UserId == ownerUserId.Value,
                cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<WishlistItem>> ListAsync(UserId ownerUserId, bool includePurchased, CancellationToken cancellationToken)
    {
        IQueryable<WishlistItemRecord> query = _dbContext.WishlistItems
            .AsNoTracking()
            .Where(x => x.UserId == ownerUserId.Value);

        if (!includePurchased)
        {
            query = query.Where(x => x.Status != WishlistItemStatus.Purchased.ToString());
        }

        var records = await query
            .Include(x => x.ExternalLinks)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return records.Select(ToDomain).ToArray();
    }

    public Task RemoveAsync(WishlistItem item, CancellationToken cancellationToken)
    {
        var tracked = _dbContext.WishlistItems.Local.FirstOrDefault(x => x.Id == item.Id.Value);
        if (tracked is not null)
        {
            _dbContext.WishlistItems.Remove(tracked);
            return Task.CompletedTask;
        }

        var stub = new WishlistItemRecord { Id = item.Id.Value };
        _dbContext.WishlistItems.Attach(stub);
        _dbContext.WishlistItems.Remove(stub);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static WishlistItemRecord ToRecord(WishlistItem item)
    {
        return new WishlistItemRecord
        {
            Id = item.Id.Value,
            UserId = item.OwnerUserId.Value,
            Category = item.Category.ToString(),
            Name = item.Name,
            Brand = item.Brand,
            TargetPrice = item.TargetPrice,
            InspirationImageAssetId = item.InspirationImageAssetId?.Value,
            Status = item.Status.ToString(),
            PurchasedAtUtc = item.PurchasedAtUtc,
            ConvertedWardrobeItemId = item.ConvertedWardrobeItemId,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc,
            ExternalLinks = item.ExternalLinks.Select(link => new WishlistExternalLinkRecord
            {
                Id = link.Id.Value,
                WishlistItemId = item.Id.Value,
                Url = link.Url,
                CreatedAtUtc = link.CreatedAtUtc
            }).ToList()
        };
    }

    private static WishlistItem ToDomain(WishlistItemRecord record)
    {
        var links = record.ExternalLinks.Select(
            x => WishlistExternalLink.Rehydrate(
                new WishlistExternalLinkId(x.Id),
                new WishlistItemId(x.WishlistItemId),
                x.Url,
                x.CreatedAtUtc));

        return WishlistItem.Rehydrate(
            new WishlistItemId(record.Id),
            new UserId(record.UserId),
            Enum.Parse<ClothingCategory>(record.Category),
            record.Name,
            record.Brand,
            record.TargetPrice,
            record.InspirationImageAssetId.HasValue ? new MediaAssetId(record.InspirationImageAssetId.Value) : null,
            Enum.Parse<WishlistItemStatus>(record.Status),
            record.PurchasedAtUtc,
            record.ConvertedWardrobeItemId,
            links,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);
    }
}
