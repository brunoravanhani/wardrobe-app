using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wardrobe;
using VirtualWardrobe.Infrastructure.Persistence.Entities;

namespace VirtualWardrobe.Infrastructure.Persistence.Configurations;

public sealed class WardrobeItemConfiguration : IEntityTypeConfiguration<WardrobeItemRecord>
{
    public void Configure(EntityTypeBuilder<WardrobeItemRecord> builder)
    {
        builder.ToTable("wardrobe_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Brand).HasColumnName("brand").HasMaxLength(120);
        builder.Property(x => x.Size).HasColumnName("size").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Price).HasColumnName("price").HasColumnType("numeric(12,2)");
        builder.Property(x => x.BodyImageAssetId).HasColumnName("body_image_asset_id");
        builder.Property(x => x.CareTagImageAssetId).HasColumnName("care_tag_image_asset_id");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.Category });
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
    }
}

public sealed class EfWardrobeItemRepository : IWardrobeItemRepository
{
    private readonly VirtualWardrobeDbContext _dbContext;

    public EfWardrobeItemRepository(VirtualWardrobeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WardrobeItem item, CancellationToken cancellationToken)
    {
        await _dbContext.WardrobeItems.AddAsync(ToRecord(item), cancellationToken);
    }

    public async Task<WardrobeItem?> GetByIdAsync(WardrobeItemId itemId, UserId ownerUserId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.WardrobeItems.SingleOrDefaultAsync(
            x => x.Id == itemId.Value && x.UserId == ownerUserId.Value,
            cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<WardrobeItem>> ListAsync(UserId ownerUserId, ClothingCategory? category, CancellationToken cancellationToken)
    {
        var query = _dbContext.WardrobeItems.AsNoTracking().Where(x => x.UserId == ownerUserId.Value);

        if (category.HasValue)
        {
            query = query.Where(x => x.Category == category.Value.ToString());
        }

        var records = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return records.Select(ToDomain).ToArray();
    }

    public Task RemoveAsync(WardrobeItem item, CancellationToken cancellationToken)
    {
        var tracked = _dbContext.WardrobeItems.Local.FirstOrDefault(x => x.Id == item.Id.Value);
        if (tracked is not null)
        {
            _dbContext.WardrobeItems.Remove(tracked);
            return Task.CompletedTask;
        }

        var stub = new WardrobeItemRecord { Id = item.Id.Value };
        _dbContext.WardrobeItems.Attach(stub);
        _dbContext.WardrobeItems.Remove(stub);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static WardrobeItemRecord ToRecord(WardrobeItem item)
    {
        return new WardrobeItemRecord
        {
            Id = item.Id.Value,
            UserId = item.OwnerUserId.Value,
            Category = item.Category.ToString(),
            Name = item.Name,
            Brand = item.Brand,
            Size = item.Size,
            Price = item.Price,
            BodyImageAssetId = item.BodyImageAssetId?.Value,
            CareTagImageAssetId = item.CareTagImageAssetId?.Value,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc
        };
    }

    private static WardrobeItem ToDomain(WardrobeItemRecord record)
    {
        return WardrobeItem.Rehydrate(
            new WardrobeItemId(record.Id),
            new UserId(record.UserId),
            Enum.Parse<ClothingCategory>(record.Category),
            record.Name,
            record.Size,
            record.Brand,
            record.Price,
            record.BodyImageAssetId.HasValue ? new MediaAssetId(record.BodyImageAssetId.Value) : null,
            record.CareTagImageAssetId.HasValue ? new MediaAssetId(record.CareTagImageAssetId.Value) : null,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);
    }
}

public sealed class EfMediaAssetRepository : IMediaAssetRepository
{
    private readonly VirtualWardrobeDbContext _dbContext;

    public EfMediaAssetRepository(VirtualWardrobeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsOwnedByAsync(MediaAssetId mediaAssetId, UserId ownerUserId, CancellationToken cancellationToken)
    {
        return _dbContext.MediaAssets.AnyAsync(
            x => x.Id == mediaAssetId.Value && x.UserId == ownerUserId.Value,
            cancellationToken);
    }
}
