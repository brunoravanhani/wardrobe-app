using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Application.Templates;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Templates;
using VirtualWardrobe.Domain.Wardrobe;

namespace VirtualWardrobe.UnitTests.Templates;

public sealed class SelectTemplateCommandTests
{
    private static readonly Guid CapsuleId = new("a1000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task SelectTemplateForFirstTimeShouldMaterializeSlots()
    {
        var userId = Guid.NewGuid();
        var template = BuildCapsuleTemplate();

        var templateRepository = new InMemoryWardrobeTemplateRepository(template);
        var slotRepository = new InMemoryTemplateSlotRepository();
        var userActiveTemplate = new InMemoryUserActiveTemplateRepository();
        var wardrobeRepository = new InMemoryWardrobeItemRepository();
        var fulfillmentService = new TemplateSlotFulfillmentService(slotRepository);
        var command = new SelectTemplateCommand(templateRepository, slotRepository, userActiveTemplate, wardrobeRepository, fulfillmentService);

        var result = await command.ExecuteAsync(new SelectTemplateInput(userId, CapsuleId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(20, slotRepository.Count);
        Assert.Equal(CapsuleId, await userActiveTemplate.GetActiveTemplateIdAsync(new UserId(userId), CancellationToken.None));
    }

    [Fact]
    public async Task SelectSameTemplateAgainShouldBeIdempotent()
    {
        var userId = Guid.NewGuid();
        var template = BuildCapsuleTemplate();

        var templateRepository = new InMemoryWardrobeTemplateRepository(template);
        var slotRepository = new InMemoryTemplateSlotRepository();
        var userActiveTemplate = new InMemoryUserActiveTemplateRepository();
        var wardrobeRepository = new InMemoryWardrobeItemRepository();
        var fulfillmentService = new TemplateSlotFulfillmentService(slotRepository);
        var command = new SelectTemplateCommand(templateRepository, slotRepository, userActiveTemplate, wardrobeRepository, fulfillmentService);

        await command.ExecuteAsync(new SelectTemplateInput(userId, CapsuleId), CancellationToken.None);
        var secondResult = await command.ExecuteAsync(new SelectTemplateInput(userId, CapsuleId), CancellationToken.None);

        Assert.True(secondResult.IsSuccess);
        Assert.Equal(20, slotRepository.Count);
    }

    [Fact]
    public async Task SelectUnknownTemplateShouldReturnNotFound()
    {
        var userId = Guid.NewGuid();
        var templateRepository = new InMemoryWardrobeTemplateRepository();
        var slotRepository = new InMemoryTemplateSlotRepository();
        var userActiveTemplate = new InMemoryUserActiveTemplateRepository();
        var wardrobeRepository = new InMemoryWardrobeItemRepository();
        var fulfillmentService = new TemplateSlotFulfillmentService(slotRepository);
        var command = new SelectTemplateCommand(templateRepository, slotRepository, userActiveTemplate, wardrobeRepository, fulfillmentService);

        var result = await command.ExecuteAsync(new SelectTemplateInput(userId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task SwitchingTemplateShouldDeleteUnfulfilledSlotsOfOldTemplate()
    {
        var userId = Guid.NewGuid();
        var capsule = BuildCapsuleTemplate();
        var trabalho = BuildTrabalhoTemplate();

        var templateRepository = new InMemoryWardrobeTemplateRepository(capsule, trabalho);
        var slotRepository = new InMemoryTemplateSlotRepository();
        var userActiveTemplate = new InMemoryUserActiveTemplateRepository();
        var wardrobeRepository = new InMemoryWardrobeItemRepository();
        var fulfillmentService = new TemplateSlotFulfillmentService(slotRepository);
        var command = new SelectTemplateCommand(templateRepository, slotRepository, userActiveTemplate, wardrobeRepository, fulfillmentService);

        await command.ExecuteAsync(new SelectTemplateInput(userId, CapsuleId), CancellationToken.None);
        Assert.Equal(20, slotRepository.Count);

        var trabalhoId = new Guid("a1000000-0000-0000-0000-000000000002");
        await command.ExecuteAsync(new SelectTemplateInput(userId, trabalhoId), CancellationToken.None);

        // Capsula unfulfilled slots removed, Trabalho slots added
        Assert.Equal(9, slotRepository.Count);
    }

    [Fact]
    public async Task SelectTemplateWithExistingItemsShouldAutoFulfillSlots()
    {
        var userId = Guid.NewGuid();
        var template = BuildCapsuleTemplate();

        var templateRepository = new InMemoryWardrobeTemplateRepository(template);
        var slotRepository = new InMemoryTemplateSlotRepository();
        var userActiveTemplate = new InMemoryUserActiveTemplateRepository();

        var tshirt = WardrobeItem.Create(WardrobeItemId.New(), new UserId(userId), ClothingCategory.TShirt, "Camiseta", "M");
        var wardrobeRepository = new InMemoryWardrobeItemRepository(tshirt);
        var fulfillmentService = new TemplateSlotFulfillmentService(slotRepository);
        var command = new SelectTemplateCommand(templateRepository, slotRepository, userActiveTemplate, wardrobeRepository, fulfillmentService);

        await command.ExecuteAsync(new SelectTemplateInput(userId, CapsuleId), CancellationToken.None);

        var openTShirtSlots = slotRepository.Slots
            .Where(s => s.Category == ClothingCategory.TShirt && !s.IsFulfilled)
            .Count();

        // 8 TShirt slots total, 1 fulfilled = 7 open
        Assert.Equal(7, openTShirtSlots);
    }

    private static WardrobeTemplate BuildCapsuleTemplate()
    {
        var id = new WardrobeTemplateId(CapsuleId);
        var definitions = new[]
        {
            new TemplateSlotDefinition(TemplateSlotDefinitionId.New(), id, ClothingCategory.TShirt, 8),
            new TemplateSlotDefinition(TemplateSlotDefinitionId.New(), id, ClothingCategory.Shirt, 3),
            new TemplateSlotDefinition(TemplateSlotDefinitionId.New(), id, ClothingCategory.Pants, 3),
            new TemplateSlotDefinition(TemplateSlotDefinitionId.New(), id, ClothingCategory.Shorts, 3),
            new TemplateSlotDefinition(TemplateSlotDefinitionId.New(), id, ClothingCategory.Shoes, 3)
        };
        return new WardrobeTemplate(id, "Capsula", definitions);
    }

    private static WardrobeTemplate BuildTrabalhoTemplate()
    {
        var id = new WardrobeTemplateId(new Guid("a1000000-0000-0000-0000-000000000002"));
        var definitions = new[]
        {
            new TemplateSlotDefinition(TemplateSlotDefinitionId.New(), id, ClothingCategory.Shirt, 5),
            new TemplateSlotDefinition(TemplateSlotDefinitionId.New(), id, ClothingCategory.Trousers, 3),
            new TemplateSlotDefinition(TemplateSlotDefinitionId.New(), id, ClothingCategory.Shoes, 1)
        };
        return new WardrobeTemplate(id, "Trabalho", definitions);
    }

    private sealed class InMemoryWardrobeTemplateRepository : IWardrobeTemplateRepository
    {
        private readonly Dictionary<Guid, WardrobeTemplate> _templates;

        public InMemoryWardrobeTemplateRepository(params WardrobeTemplate[] templates)
        {
            _templates = templates.ToDictionary(t => t.Id.Value);
        }

        public Task<IReadOnlyList<WardrobeTemplate>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<WardrobeTemplate>>(_templates.Values.ToArray());

        public Task<WardrobeTemplate?> GetByIdAsync(WardrobeTemplateId templateId, CancellationToken cancellationToken)
        {
            _templates.TryGetValue(templateId.Value, out var template);
            return Task.FromResult(template);
        }
    }

    internal sealed class InMemoryTemplateSlotRepository : ITemplateSlotRepository
    {
        private readonly List<TemplateSlot> _slots = [];

        public IReadOnlyList<TemplateSlot> Slots => _slots;
        public int Count => _slots.Count;

        public Task AddRangeAsync(IEnumerable<TemplateSlot> slots, CancellationToken cancellationToken)
        {
            _slots.AddRange(slots);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TemplateSlot slot, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<TemplateSlot?> GetByIdAsync(TemplateSlotId slotId, UserId ownerUserId, CancellationToken cancellationToken)
        {
            var slot = _slots.FirstOrDefault(s => s.Id == slotId && s.OwnerUserId == ownerUserId);
            return Task.FromResult(slot);
        }

        public Task<TemplateSlot?> GetByWardrobeItemIdAsync(WardrobeItemId wardrobeItemId, CancellationToken cancellationToken)
        {
            var slot = _slots.FirstOrDefault(s => s.WardrobeItemId == wardrobeItemId);
            return Task.FromResult(slot);
        }

        public Task<IReadOnlyList<TemplateSlot>> ListByUserAndTemplateAsync(UserId userId, WardrobeTemplateId templateId, CancellationToken cancellationToken)
        {
            var result = _slots
                .Where(s => s.OwnerUserId == userId && s.TemplateId == templateId)
                .ToArray();
            return Task.FromResult<IReadOnlyList<TemplateSlot>>(result);
        }

        public Task<IReadOnlyList<TemplateSlot>> ListOpenByUserAndCategoryAsync(UserId userId, ClothingCategory category, CancellationToken cancellationToken)
        {
            var result = _slots
                .Where(s => s.OwnerUserId == userId && s.Category == category && !s.IsFulfilled)
                .OrderBy(s => s.CreatedAtUtc)
                .ToArray();
            return Task.FromResult<IReadOnlyList<TemplateSlot>>(result);
        }

        public Task DeleteUnfulfilledByUserAndTemplateAsync(UserId userId, WardrobeTemplateId templateId, CancellationToken cancellationToken)
        {
            _slots.RemoveAll(s => s.OwnerUserId == userId && s.TemplateId == templateId && !s.IsFulfilled);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class InMemoryUserActiveTemplateRepository : IUserActiveTemplateRepository
    {
        private readonly Dictionary<Guid, Guid?> _activeTemplates = [];

        public Task<Guid?> GetActiveTemplateIdAsync(UserId userId, CancellationToken cancellationToken)
        {
            _activeTemplates.TryGetValue(userId.Value, out var id);
            return Task.FromResult(id);
        }

        public Task SetActiveTemplateIdAsync(UserId userId, Guid? templateId, CancellationToken cancellationToken)
        {
            _activeTemplates[userId.Value] = templateId;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryWardrobeItemRepository : IWardrobeItemRepository
    {
        private readonly List<WardrobeItem> _items;

        public InMemoryWardrobeItemRepository(params WardrobeItem[] items)
        {
            _items = items.ToList();
        }

        public Task AddAsync(WardrobeItem item, CancellationToken cancellationToken)
        {
            _items.Add(item);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(WardrobeItem item, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<WardrobeItem?> GetByIdAsync(WardrobeItemId itemId, UserId ownerUserId, CancellationToken cancellationToken)
        {
            var item = _items.FirstOrDefault(x => x.Id == itemId && x.OwnerUserId == ownerUserId);
            return Task.FromResult(item);
        }

        public Task<IReadOnlyList<WardrobeItem>> ListAsync(UserId ownerUserId, ClothingCategory? category, CancellationToken cancellationToken)
        {
            var query = _items.Where(x => x.OwnerUserId == ownerUserId);
            if (category.HasValue)
            {
                query = query.Where(x => x.Category == category.Value);
            }
            return Task.FromResult<IReadOnlyList<WardrobeItem>>(query.ToArray());
        }

        public Task RemoveAsync(WardrobeItem item, CancellationToken cancellationToken)
        {
            _items.RemoveAll(x => x.Id == item.Id);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
