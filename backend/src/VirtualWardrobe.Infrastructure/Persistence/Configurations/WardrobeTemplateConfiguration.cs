using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualWardrobe.Application.Templates;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Templates;
using VirtualWardrobe.Infrastructure.Persistence.Entities;

namespace VirtualWardrobe.Infrastructure.Persistence.Configurations;

public sealed class WardrobeTemplateConfiguration : IEntityTypeConfiguration<WardrobeTemplateRecord>
{
    public void Configure(EntityTypeBuilder<WardrobeTemplateRecord> builder)
    {
        builder.ToTable("wardrobe_templates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();

        builder.HasMany(x => x.SlotDefinitions)
            .WithOne(x => x.Template)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TemplateSlots)
            .WithOne(x => x.Template)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TemplateSlotDefinitionConfiguration : IEntityTypeConfiguration<TemplateSlotDefinitionRecord>
{
    public void Configure(EntityTypeBuilder<TemplateSlotDefinitionRecord> builder)
    {
        builder.ToTable("template_slot_definitions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TemplateId).HasColumnName("template_id").IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();

        builder.HasOne(x => x.Template)
            .WithMany(x => x.SlotDefinitions)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TemplateId);
    }
}

public sealed class TemplateSlotConfiguration : IEntityTypeConfiguration<TemplateSlotRecord>
{
    public void Configure(EntityTypeBuilder<TemplateSlotRecord> builder)
    {
        builder.ToTable("template_slots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TemplateId).HasColumnName("template_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(32).IsRequired();
        builder.Property(x => x.WardrobeItemId).HasColumnName("wardrobe_item_id");
        builder.Property(x => x.WishlistItemId).HasColumnName("wishlist_item_id");
        builder.Property(x => x.FulfilledAtUtc).HasColumnName("fulfilled_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne(x => x.Template)
            .WithMany(x => x.TemplateSlots)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.TemplateId });
        builder.HasIndex(x => new { x.UserId, x.Category });
        builder.HasIndex(x => x.WardrobeItemId)
            .IsUnique()
            .HasFilter("wardrobe_item_id IS NOT NULL");
    }
}

public sealed class EfWardrobeTemplateRepository : IWardrobeTemplateRepository
{
    private readonly VirtualWardrobeDbContext _dbContext;

    public EfWardrobeTemplateRepository(VirtualWardrobeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<WardrobeTemplate>> GetAllAsync(CancellationToken cancellationToken)
    {
        var records = await _dbContext.WardrobeTemplates
            .AsNoTracking()
            .Include(x => x.SlotDefinitions)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return records.Select(ToDomain).ToArray();
    }

    public async Task<WardrobeTemplate?> GetByIdAsync(WardrobeTemplateId templateId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.WardrobeTemplates
            .AsNoTracking()
            .Include(x => x.SlotDefinitions)
            .SingleOrDefaultAsync(x => x.Id == templateId.Value, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    private static WardrobeTemplate ToDomain(WardrobeTemplateRecord record)
    {
        var definitions = record.SlotDefinitions
            .Select(d => new TemplateSlotDefinition(
                new TemplateSlotDefinitionId(d.Id),
                new WardrobeTemplateId(d.TemplateId),
                Enum.Parse<ClothingCategory>(d.Category),
                d.Quantity))
            .ToArray();

        return new WardrobeTemplate(new WardrobeTemplateId(record.Id), record.Name, definitions);
    }
}

public sealed class EfTemplateSlotRepository : ITemplateSlotRepository
{
    private readonly VirtualWardrobeDbContext _dbContext;

    public EfTemplateSlotRepository(VirtualWardrobeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRangeAsync(IEnumerable<TemplateSlot> slots, CancellationToken cancellationToken)
    {
        var records = slots.Select(ToRecord).ToList();
        await _dbContext.TemplateSlots.AddRangeAsync(records, cancellationToken);
    }

    public async Task UpdateAsync(TemplateSlot slot, CancellationToken cancellationToken)
    {
        var record = _dbContext.TemplateSlots.Local.FirstOrDefault(x => x.Id == slot.Id.Value)
                     ?? await _dbContext.TemplateSlots.SingleOrDefaultAsync(x => x.Id == slot.Id.Value, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.WardrobeItemId = slot.WardrobeItemId?.Value;
        record.WishlistItemId = slot.WishlistItemId?.Value;
        record.FulfilledAtUtc = slot.FulfilledAtUtc;
        record.UpdatedAtUtc = slot.UpdatedAtUtc;
    }

    public async Task<TemplateSlot?> GetByIdAsync(TemplateSlotId slotId, UserId ownerUserId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.TemplateSlots
            .SingleOrDefaultAsync(x => x.Id == slotId.Value && x.UserId == ownerUserId.Value, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<TemplateSlot?> GetByWardrobeItemIdAsync(WardrobeItemId wardrobeItemId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.TemplateSlots
            .SingleOrDefaultAsync(x => x.WardrobeItemId == wardrobeItemId.Value, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<TemplateSlot>> ListByUserAndTemplateAsync(
        UserId userId,
        WardrobeTemplateId templateId,
        CancellationToken cancellationToken)
    {
        var records = await _dbContext.TemplateSlots
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value && x.TemplateId == templateId.Value)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return records.Select(ToDomain).ToArray();
    }

    public async Task<IReadOnlyList<TemplateSlot>> ListOpenByUserAndCategoryAsync(
        UserId userId,
        ClothingCategory category,
        CancellationToken cancellationToken)
    {
        var categoryStr = category.ToString();
        var records = await _dbContext.TemplateSlots
            .Where(x => x.UserId == userId.Value && x.Category == categoryStr && x.WardrobeItemId == null)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return records.Select(ToDomain).ToArray();
    }

    public async Task DeleteUnfulfilledByUserAndTemplateAsync(
        UserId userId,
        WardrobeTemplateId templateId,
        CancellationToken cancellationToken)
    {
        var records = await _dbContext.TemplateSlots
            .Where(x => x.UserId == userId.Value && x.TemplateId == templateId.Value && x.WardrobeItemId == null)
            .ToListAsync(cancellationToken);

        _dbContext.TemplateSlots.RemoveRange(records);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TemplateSlotRecord ToRecord(TemplateSlot slot)
    {
        return new TemplateSlotRecord
        {
            Id = slot.Id.Value,
            TemplateId = slot.TemplateId.Value,
            UserId = slot.OwnerUserId.Value,
            Category = slot.Category.ToString(),
            WardrobeItemId = slot.WardrobeItemId?.Value,
            WishlistItemId = slot.WishlistItemId?.Value,
            FulfilledAtUtc = slot.FulfilledAtUtc,
            CreatedAtUtc = slot.CreatedAtUtc,
            UpdatedAtUtc = slot.UpdatedAtUtc
        };
    }

    private static TemplateSlot ToDomain(TemplateSlotRecord record)
    {
        return TemplateSlot.Rehydrate(
            new TemplateSlotId(record.Id),
            new WardrobeTemplateId(record.TemplateId),
            new UserId(record.UserId),
            Enum.Parse<ClothingCategory>(record.Category),
            record.WardrobeItemId.HasValue ? new WardrobeItemId(record.WardrobeItemId.Value) : null,
            record.WishlistItemId.HasValue ? new WishlistItemId(record.WishlistItemId.Value) : null,
            record.FulfilledAtUtc,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);
    }
}

public sealed class EfUserActiveTemplateRepository : IUserActiveTemplateRepository
{
    private readonly VirtualWardrobeDbContext _dbContext;

    public EfUserActiveTemplateRepository(VirtualWardrobeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid?> GetActiveTemplateIdAsync(UserId userId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId.Value)
            .Select(x => new { x.ActiveTemplateId })
            .SingleOrDefaultAsync(cancellationToken);

        return record?.ActiveTemplateId;
    }

    public async Task SetActiveTemplateIdAsync(UserId userId, Guid? templateId, CancellationToken cancellationToken)
    {
        var record = _dbContext.Users.Local.FirstOrDefault(x => x.Id == userId.Value)
                     ?? await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.ActiveTemplateId = templateId;
    }
}
