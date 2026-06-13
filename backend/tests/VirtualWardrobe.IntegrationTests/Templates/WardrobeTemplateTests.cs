using Microsoft.EntityFrameworkCore;
using VirtualWardrobe.Application.Templates;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Infrastructure.Persistence;
using VirtualWardrobe.Infrastructure.Persistence.Configurations;
using VirtualWardrobe.Infrastructure.Persistence.Entities;

namespace VirtualWardrobe.IntegrationTests.Templates;

public sealed class WardrobeTemplateTests
{
    private static readonly Guid CapsuleId = new("a1000000-0000-0000-0000-000000000001");
    private static readonly Guid TrabalhoId = new("a1000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task GetAllTemplatesShouldReturnSeededTemplates()
    {
        await using var dbContext = CreateDbContext();
        await SeedTemplatesAsync(dbContext);

        var repository = new EfWardrobeTemplateRepository(dbContext);
        var templates = await repository.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, templates.Count);
        Assert.Contains(templates, t => t.Name == "Capsula");
        Assert.Contains(templates, t => t.Name == "Trabalho");
    }

    [Fact]
    public async Task SelectCapsuleTemplateShouldMaterialize20Slots()
    {
        await using var dbContext = CreateDbContext();
        await SeedTemplatesAsync(dbContext);
        var userId = await SeedUserAsync(dbContext);

        var command = CreateSelectTemplateCommand(dbContext);
        var result = await command.ExecuteAsync(new SelectTemplateInput(userId, CapsuleId), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var slotCount = await dbContext.TemplateSlots.CountAsync(x => x.UserId == userId);
        Assert.Equal(20, slotCount);

        var user = await dbContext.Users.FindAsync(userId);
        Assert.Equal(CapsuleId, user!.ActiveTemplateId);
    }

    [Fact]
    public async Task SelectTrabalhoTemplateShouldMaterialize9Slots()
    {
        await using var dbContext = CreateDbContext();
        await SeedTemplatesAsync(dbContext);
        var userId = await SeedUserAsync(dbContext);

        var command = CreateSelectTemplateCommand(dbContext);
        var result = await command.ExecuteAsync(new SelectTemplateInput(userId, TrabalhoId), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var slotCount = await dbContext.TemplateSlots.CountAsync(x => x.UserId == userId);
        Assert.Equal(9, slotCount);
    }

    [Fact]
    public async Task SwitchingTemplateShouldDeleteUnfulfilledCapsuleSlots()
    {
        await using var dbContext = CreateDbContext();
        await SeedTemplatesAsync(dbContext);
        var userId = await SeedUserAsync(dbContext);

        var command = CreateSelectTemplateCommand(dbContext);
        await command.ExecuteAsync(new SelectTemplateInput(userId, CapsuleId), CancellationToken.None);

        var result = await command.ExecuteAsync(new SelectTemplateInput(userId, TrabalhoId), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var slotCount = await dbContext.TemplateSlots.CountAsync(x => x.UserId == userId);
        Assert.Equal(9, slotCount);

        var user = await dbContext.Users.FindAsync(userId);
        Assert.Equal(TrabalhoId, user!.ActiveTemplateId);
    }

    [Fact]
    public async Task AddingWardrobeItemShouldAutoFulfillOldestOpenSlot()
    {
        await using var dbContext = CreateDbContext();
        await SeedTemplatesAsync(dbContext);
        var userId = await SeedUserAsync(dbContext);

        var selectCommand = CreateSelectTemplateCommand(dbContext);
        await selectCommand.ExecuteAsync(new SelectTemplateInput(userId, CapsuleId), CancellationToken.None);

        var createCommand = CreateWardrobeItemCommand(dbContext);
        var createResult = await createCommand.CreateAsync(
            new CreateWardrobeItemInput(userId, ClothingCategory.TShirt, "Camiseta", "M", null, null, null, null),
            CancellationToken.None);

        Assert.True(createResult.IsSuccess);

        var fulfilledSlots = await dbContext.TemplateSlots
            .CountAsync(x => x.UserId == userId && x.WardrobeItemId != null);
        Assert.Equal(1, fulfilledSlots);

        var openTShirtSlots = await dbContext.TemplateSlots
            .CountAsync(x => x.UserId == userId && x.Category == "TShirt" && x.WardrobeItemId == null);
        Assert.Equal(7, openTShirtSlots);
    }

    [Fact]
    public async Task DeletingFulfilledWardrobeItemShouldRevertSlotToOpen()
    {
        await using var dbContext = CreateDbContext();
        await SeedTemplatesAsync(dbContext);
        var userId = await SeedUserAsync(dbContext);

        var selectCommand = CreateSelectTemplateCommand(dbContext);
        await selectCommand.ExecuteAsync(new SelectTemplateInput(userId, CapsuleId), CancellationToken.None);

        var createCommand = CreateWardrobeItemCommand(dbContext);
        var createResult = await createCommand.CreateAsync(
            new CreateWardrobeItemInput(userId, ClothingCategory.Shirt, "Camisa", "G", null, null, null, null),
            CancellationToken.None);

        Assert.True(createResult.IsSuccess);
        var wardrobeItemId = createResult.Value.Id.Value;

        var deleteResult = await createCommand.DeleteAsync(wardrobeItemId, userId, CancellationToken.None);
        Assert.True(deleteResult.IsSuccess);

        var fulfilledSlots = await dbContext.TemplateSlots
            .CountAsync(x => x.UserId == userId && x.WardrobeItemId != null);
        Assert.Equal(0, fulfilledSlots);
    }

    private static SelectTemplateCommand CreateSelectTemplateCommand(VirtualWardrobeDbContext dbContext)
    {
        var templateRepository = new EfWardrobeTemplateRepository(dbContext);
        var slotRepository = new EfTemplateSlotRepository(dbContext);
        var userActiveTemplate = new EfUserActiveTemplateRepository(dbContext);
        var wardrobeRepository = new EfWardrobeItemRepository(dbContext);
        var fulfillmentService = new TemplateSlotFulfillmentService(slotRepository);
        return new SelectTemplateCommand(templateRepository, slotRepository, userActiveTemplate, wardrobeRepository, fulfillmentService);
    }

    private static CreateWardrobeItemCommand CreateWardrobeItemCommand(VirtualWardrobeDbContext dbContext)
    {
        var wardrobeRepository = new EfWardrobeItemRepository(dbContext);
        var mediaRepository = new EfMediaAssetRepository(dbContext);
        var slotRepository = new EfTemplateSlotRepository(dbContext);
        var fulfillmentService = new TemplateSlotFulfillmentService(slotRepository);
        return new CreateWardrobeItemCommand(wardrobeRepository, mediaRepository, new NoOpMediaUrlService(), fulfillmentService);
    }

    private static async Task SeedTemplatesAsync(VirtualWardrobeDbContext dbContext)
    {
        dbContext.WardrobeTemplates.AddRange(
            new WardrobeTemplateRecord
            {
                Id = CapsuleId,
                Name = "Capsula",
                SlotDefinitions =
                [
                    new TemplateSlotDefinitionRecord { Id = Guid.NewGuid(), TemplateId = CapsuleId, Category = "TShirt", Quantity = 8 },
                    new TemplateSlotDefinitionRecord { Id = Guid.NewGuid(), TemplateId = CapsuleId, Category = "Shirt", Quantity = 3 },
                    new TemplateSlotDefinitionRecord { Id = Guid.NewGuid(), TemplateId = CapsuleId, Category = "Pants", Quantity = 3 },
                    new TemplateSlotDefinitionRecord { Id = Guid.NewGuid(), TemplateId = CapsuleId, Category = "Shorts", Quantity = 3 },
                    new TemplateSlotDefinitionRecord { Id = Guid.NewGuid(), TemplateId = CapsuleId, Category = "Shoes", Quantity = 3 }
                ]
            },
            new WardrobeTemplateRecord
            {
                Id = TrabalhoId,
                Name = "Trabalho",
                SlotDefinitions =
                [
                    new TemplateSlotDefinitionRecord { Id = Guid.NewGuid(), TemplateId = TrabalhoId, Category = "Shirt", Quantity = 5 },
                    new TemplateSlotDefinitionRecord { Id = Guid.NewGuid(), TemplateId = TrabalhoId, Category = "Trousers", Quantity = 3 },
                    new TemplateSlotDefinitionRecord { Id = Guid.NewGuid(), TemplateId = TrabalhoId, Category = "Shoes", Quantity = 1 }
                ]
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Guid> SeedUserAsync(VirtualWardrobeDbContext dbContext)
    {
        var userId = Guid.NewGuid();
        dbContext.Users.Add(new UserRecord
        {
            Id = userId,
            GoogleSubject = $"google|{userId}",
            Email = $"user-{userId}@test.com",
            Locale = "pt-BR",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        return userId;
    }

    private static VirtualWardrobeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VirtualWardrobeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new VirtualWardrobeDbContext(options);
    }

    private sealed class NoOpMediaUrlService : Application.Storage.IPrivateMediaUrlService
    {
        public Task<Application.Storage.PresignedUploadResult> CreateUploadUrlAsync(Application.Storage.PresignedUploadRequest request, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<Application.Storage.PresignedViewResult> CreateViewUrlAsync(Guid mediaAssetId, Guid ownerUserId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task DeleteMediaAssetAsync(Guid mediaAssetId, Guid ownerUserId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
