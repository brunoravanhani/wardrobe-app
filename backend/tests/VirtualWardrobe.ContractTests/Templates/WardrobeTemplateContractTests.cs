using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using VirtualWardrobe.Api.Controllers;
using VirtualWardrobe.Application.Templates;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Application.Wishlist;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Templates;
using VirtualWardrobe.Domain.Wardrobe;

namespace VirtualWardrobe.ContractTests.Templates;

public sealed class WardrobeTemplateContractTests
{
    private static readonly Guid CapsuleId = new("a1000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task GetTemplatesShouldReturnAllTemplatesWithDefinitions()
    {
        var template = BuildCapsuleTemplate();
        var getTemplatesQuery = new GetTemplatesQuery(new InMemoryWardrobeTemplateRepository(template));
        var getUserSlotsQuery = new GetUserSlotsQuery(new InMemoryTemplateSlotRepository(), new InMemoryUserActiveTemplateRepository());
        var selectCommand = BuildSelectCommand(template);
        var linkCommand = new LinkSlotToWishlistCommand(new InMemoryTemplateSlotRepository(), new InMemoryWishlistItemRepository());

        var controller = new WardrobeTemplatesController(
            getTemplatesQuery, getUserSlotsQuery, selectCommand, linkCommand);

        var action = await controller.GetTemplatesAsync(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var templates = Assert.IsAssignableFrom<IReadOnlyList<WardrobeTemplateResponse>>(ok.Value);
        Assert.Single(templates);
        Assert.Equal("Capsula", templates[0].Name);
        Assert.Equal(5, templates[0].SlotDefinitions.Count);
    }

    [Fact]
    public async Task SelectTemplateShouldReturn204AndMaterializeSlots()
    {
        var ownerUserId = Guid.NewGuid();
        var template = BuildCapsuleTemplate();
        var slotRepository = new InMemoryTemplateSlotRepository();
        var userActiveTemplate = new InMemoryUserActiveTemplateRepository();

        var getTemplatesQuery = new GetTemplatesQuery(new InMemoryWardrobeTemplateRepository(template));
        var getUserSlotsQuery = new GetUserSlotsQuery(slotRepository, userActiveTemplate);
        var selectCommand = BuildSelectCommand(template, slotRepository, userActiveTemplate);
        var linkCommand = new LinkSlotToWishlistCommand(slotRepository, new InMemoryWishlistItemRepository());

        var controller = new WardrobeTemplatesController(
            getTemplatesQuery, getUserSlotsQuery, selectCommand, linkCommand);
        AttachUser(controller, ownerUserId);

        var action = await controller.SelectTemplateAsync(CapsuleId, CancellationToken.None);

        Assert.IsType<NoContentResult>(action);
        Assert.Equal(20, slotRepository.Count);
    }

    [Fact]
    public async Task GetUserSlotsShouldReturnSlotsForActiveTemplate()
    {
        var ownerUserId = Guid.NewGuid();
        var template = BuildCapsuleTemplate();
        var slotRepository = new InMemoryTemplateSlotRepository();
        var userActiveTemplate = new InMemoryUserActiveTemplateRepository();

        var getTemplatesQuery = new GetTemplatesQuery(new InMemoryWardrobeTemplateRepository(template));
        var getUserSlotsQuery = new GetUserSlotsQuery(slotRepository, userActiveTemplate);
        var selectCommand = BuildSelectCommand(template, slotRepository, userActiveTemplate);
        var linkCommand = new LinkSlotToWishlistCommand(slotRepository, new InMemoryWishlistItemRepository());

        var controller = new WardrobeTemplatesController(
            getTemplatesQuery, getUserSlotsQuery, selectCommand, linkCommand);
        AttachUser(controller, ownerUserId);

        await controller.SelectTemplateAsync(CapsuleId, CancellationToken.None);

        var slotsAction = await controller.GetUserSlotsAsync(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(slotsAction.Result);
        var response = Assert.IsType<UserSlotsResponse>(ok.Value);

        Assert.Equal(CapsuleId, response.ActiveTemplateId);
        Assert.Equal(20, response.Slots.Count);
        Assert.All(response.Slots, s => Assert.False(s.IsFulfilled));
    }

    [Fact]
    public async Task SelectUnknownTemplateShouldReturn404()
    {
        var ownerUserId = Guid.NewGuid();
        var getTemplatesQuery = new GetTemplatesQuery(new InMemoryWardrobeTemplateRepository());
        var getUserSlotsQuery = new GetUserSlotsQuery(new InMemoryTemplateSlotRepository(), new InMemoryUserActiveTemplateRepository());
        var selectCommand = BuildSelectCommand();
        var linkCommand = new LinkSlotToWishlistCommand(new InMemoryTemplateSlotRepository(), new InMemoryWishlistItemRepository());

        var controller = new WardrobeTemplatesController(
            getTemplatesQuery, getUserSlotsQuery, selectCommand, linkCommand);
        AttachUser(controller, ownerUserId);

        var action = await controller.SelectTemplateAsync(Guid.NewGuid(), CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(action);
        Assert.Equal(404, problem.StatusCode);
    }

    private static SelectTemplateCommand BuildSelectCommand(
        WardrobeTemplate? template = null,
        InMemoryTemplateSlotRepository? slotRepository = null,
        InMemoryUserActiveTemplateRepository? userActiveTemplate = null)
    {
        var templates = template is null ? new InMemoryWardrobeTemplateRepository() : new InMemoryWardrobeTemplateRepository(template);
        var slots = slotRepository ?? new InMemoryTemplateSlotRepository();
        var active = userActiveTemplate ?? new InMemoryUserActiveTemplateRepository();
        var fulfillmentService = new TemplateSlotFulfillmentService(slots);
        return new SelectTemplateCommand(templates, slots, active, new InMemoryWardrobeItemRepository(), fulfillmentService);
    }

    private static void AttachUser(ControllerBase controller, Guid userId)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                ], "test"))
            }
        };
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
            _templates.TryGetValue(templateId.Value, out var t);
            return Task.FromResult(t);
        }
    }

    internal sealed class InMemoryTemplateSlotRepository : ITemplateSlotRepository
    {
        private readonly List<TemplateSlot> _slots = [];
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
            var result = _slots.Where(s => s.OwnerUserId == userId && s.TemplateId == templateId).ToArray();
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
        private readonly Dictionary<Guid, Guid?> _active = [];

        public Task<Guid?> GetActiveTemplateIdAsync(UserId userId, CancellationToken cancellationToken)
        {
            _active.TryGetValue(userId.Value, out var id);
            return Task.FromResult(id);
        }

        public Task SetActiveTemplateIdAsync(UserId userId, Guid? templateId, CancellationToken cancellationToken)
        {
            _active[userId.Value] = templateId;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryWardrobeItemRepository : IWardrobeItemRepository
    {
        public Task AddAsync(WardrobeItem item, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAsync(WardrobeItem item, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<WardrobeItem?> GetByIdAsync(WardrobeItemId itemId, UserId ownerUserId, CancellationToken cancellationToken) => Task.FromResult<WardrobeItem?>(null);
        public Task<IReadOnlyList<WardrobeItem>> ListAsync(UserId ownerUserId, ClothingCategory? category, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<WardrobeItem>>(Array.Empty<WardrobeItem>());
        public Task RemoveAsync(WardrobeItem item, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryWishlistItemRepository : IWishlistItemRepository
    {
        private readonly Dictionary<Guid, Domain.Wishlist.WishlistItem> _items = [];

        public Task AddAsync(Domain.Wishlist.WishlistItem item, CancellationToken cancellationToken)
        {
            _items[item.Id.Value] = item;
            return Task.CompletedTask;
        }
        public Task UpdateAsync(Domain.Wishlist.WishlistItem item, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Domain.Wishlist.WishlistItem?> GetByIdAsync(WishlistItemId itemId, UserId ownerUserId, CancellationToken cancellationToken) => Task.FromResult<Domain.Wishlist.WishlistItem?>(null);
        public Task<IReadOnlyList<Domain.Wishlist.WishlistItem>> ListAsync(UserId ownerUserId, bool includePurchased, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Domain.Wishlist.WishlistItem>>(Array.Empty<Domain.Wishlist.WishlistItem>());
        public Task RemoveAsync(Domain.Wishlist.WishlistItem item, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
