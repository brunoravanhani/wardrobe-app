using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VirtualWardrobe.Api.Controllers;
using VirtualWardrobe.Application.Auth;
using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Application.Storage;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wardrobe;

namespace VirtualWardrobe.ContractTests.Wardrobe;

public sealed class WardrobeContractTests
{
    [Fact]
    public async Task AuthExchangeShouldReturnSessionPayload()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            SigningKey = "super-secret-signing-key-with-32-chars",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 30
        });

        var authService = new AuthSessionService(
            new FakeGoogleTokenVerifier(),
            new FakeUserIdentityStore(),
            jwtOptions);

        var controller = new AuthController(authService, Microsoft.Extensions.Logging.Abstractions.NullLogger<VirtualWardrobe.Api.Controllers.AuthController>.Instance);

        var action = await controller.ExchangeAsync(new ExchangeGoogleTokenRequest("valid-token"), CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var payload = Assert.IsType<AuthSessionResponse>(ok.Value);

        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.Equal("pt-BR", payload.User.Locale);
    }

    [Fact]
    public async Task WardrobeCrudContractShouldSupportCreateListUpdateDelete()
    {
        var ownerUserId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();

        var mediaRepository = new InMemoryMediaAssetRepository(ownerUserId, mediaId);
        var wardrobeRepository = new InMemoryWardrobeItemRepository();
        var command = new CreateWardrobeItemCommand(wardrobeRepository, mediaRepository, new FakePrivateMediaUrlService());

        var controller = new WardrobeItemsController(command);
        AttachUser(controller, ownerUserId);

        var createAction = await controller.CreateAsync(
            new CreateWardrobeItemRequest(
                "TShirt",
                "Camiseta",
                "M",
                "Marca",
                50m,
                mediaId,
                null),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(createAction.Result);
        var createdItem = Assert.IsType<WardrobeItemResponse>(created.Value);

        var updateAction = await controller.UpdateAsync(
            createdItem.Id,
            new UpdateWardrobeItemRequest(
                "Shirt",
                "Camisa",
                "G",
                "Outra",
                70m,
                mediaId,
                null),
            CancellationToken.None);

        var updated = Assert.IsType<OkObjectResult>(updateAction.Result);
        _ = Assert.IsType<WardrobeItemResponse>(updated.Value);

        var listAction = await controller.ListAsync(null, CancellationToken.None);
        var listed = Assert.IsType<OkObjectResult>(listAction.Result);
        var listPayload = Assert.IsType<WardrobeItemResponse[]>(listed.Value);
        Assert.Single(listPayload);

        var deleteAction = await controller.DeleteAsync(createdItem.Id, CancellationToken.None);
        Assert.IsType<NoContentResult>(deleteAction);
    }

    [Fact]
    public async Task MediaPresignContractShouldReturnUploadAndViewUrls()
    {
        var ownerUserId = Guid.NewGuid();
        var mediaService = new FakePrivateMediaUrlService();
        var controller = new MediaController(mediaService, Microsoft.Extensions.Logging.Abstractions.NullLogger<VirtualWardrobe.Api.Controllers.MediaController>.Instance);
        AttachUser(controller, ownerUserId);

        var uploadAction = await controller.CreateUploadUrlAsync(
            new CreateUploadUrlRequest("foto.jpg", "image/jpeg", 1024, "WardrobeBodyImage"),
            CancellationToken.None);

        var uploadOk = Assert.IsType<OkObjectResult>(uploadAction.Result);
        var uploadPayload = Assert.IsType<CreateUploadUrlResponse>(uploadOk.Value);

        var viewAction = await controller.CreateViewUrlAsync(uploadPayload.MediaAssetId, CancellationToken.None);
        var viewOk = Assert.IsType<OkObjectResult>(viewAction.Result);
        _ = Assert.IsType<CreateViewUrlResponse>(viewOk.Value);
    }

    private static void AttachUser(ControllerBase controller, Guid userId)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    ],
                    authenticationType: "test"))
            }
        };
    }

    private sealed class FakeGoogleTokenVerifier : IGoogleTokenVerifier
    {
        public Task<GoogleIdentityProfile> VerifyAsync(string idToken, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GoogleIdentityProfile("sub-123", "user@test.com", "Test User"));
        }
    }

    private sealed class FakeUserIdentityStore : IUserIdentityStore
    {
        public Task<AuthenticatedUser> GetOrCreateAsync(GoogleIdentityProfile profile, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AuthenticatedUser(Guid.NewGuid(), profile.Subject, profile.Email, profile.DisplayName, "pt-BR"));
        }
    }

    private sealed class InMemoryMediaAssetRepository : IMediaAssetRepository
    {
        private readonly Guid _owner;
        private readonly HashSet<Guid> _ownedMediaIds;

        public InMemoryMediaAssetRepository(Guid owner, params Guid[] ownedMediaIds)
        {
            _owner = owner;
            _ownedMediaIds = ownedMediaIds.ToHashSet();
        }

        public Task<bool> ExistsOwnedByAsync(MediaAssetId mediaAssetId, UserId ownerUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ownerUserId.Value == _owner && _ownedMediaIds.Contains(mediaAssetId.Value));
        }
    }

    private sealed class InMemoryWardrobeItemRepository : IWardrobeItemRepository
    {
        private readonly Dictionary<Guid, WardrobeItem> _items = [];

        public Task AddAsync(WardrobeItem item, CancellationToken cancellationToken)
        {
            _items[item.Id.Value] = item;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(WardrobeItem item, CancellationToken cancellationToken)
        {
            _items[item.Id.Value] = item;
            return Task.CompletedTask;
        }

        public Task<WardrobeItem?> GetByIdAsync(WardrobeItemId itemId, UserId ownerUserId, CancellationToken cancellationToken)
        {
            if (_items.TryGetValue(itemId.Value, out var item) && item.OwnerUserId == ownerUserId)
            {
                return Task.FromResult<WardrobeItem?>(item);
            }

            return Task.FromResult<WardrobeItem?>(null);
        }

        public Task<IReadOnlyList<WardrobeItem>> ListAsync(UserId ownerUserId, ClothingCategory? category, CancellationToken cancellationToken)
        {
            var query = _items.Values.Where(x => x.OwnerUserId == ownerUserId);
            if (category.HasValue)
            {
                query = query.Where(x => x.Category == category.Value);
            }

            return Task.FromResult<IReadOnlyList<WardrobeItem>>(query.ToArray());
        }

        public Task RemoveAsync(WardrobeItem item, CancellationToken cancellationToken)
        {
            _items.Remove(item.Id.Value);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakePrivateMediaUrlService : IPrivateMediaUrlService
    {
        public Task<PresignedUploadResult> CreateUploadUrlAsync(PresignedUploadRequest request, CancellationToken cancellationToken)
        {
            var response = new PresignedUploadResult(
                Guid.NewGuid(),
                "users/x/media/y/foto.jpg",
                new Uri("https://example.com/upload"),
                DateTime.UtcNow.AddMinutes(10),
                new Dictionary<string, string> { ["Content-Type"] = request.ContentType });

            return Task.FromResult(response);
        }

        public Task<PresignedViewResult> CreateViewUrlAsync(Guid mediaAssetId, Guid ownerUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PresignedViewResult(new Uri("https://example.com/view"), DateTime.UtcNow.AddMinutes(10)));
        }

        public Task DeleteMediaAssetAsync(Guid mediaAssetId, Guid ownerUserId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
