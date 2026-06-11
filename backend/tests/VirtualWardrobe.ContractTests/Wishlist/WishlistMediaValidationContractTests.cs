using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VirtualWardrobe.Api.Controllers;
using VirtualWardrobe.Application.Storage;

namespace VirtualWardrobe.ContractTests.Wishlist;

public sealed class WishlistMediaValidationContractTests
{
    [Theory]
    [InlineData("image/gif", 1024)]
    [InlineData("image/jpeg", 11 * 1024 * 1024)]
    public async Task UploadUrlForWishlistInspirationWhenMediaConstraintsAreInvalidShouldReturnBadRequest(
        string contentType,
        long fileSizeBytes)
    {
        var controller = new MediaController(new ValidatingPrivateMediaUrlService(), Microsoft.Extensions.Logging.Abstractions.NullLogger<VirtualWardrobe.Api.Controllers.MediaController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                    ],
                    "test"))
            }
        };

        var action = await controller.CreateUploadUrlAsync(
            new CreateUploadUrlRequest("inspiration.bin", contentType, fileSizeBytes, "WishlistInspirationImage"),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(action.Result);
    }

    private sealed class ValidatingPrivateMediaUrlService : IPrivateMediaUrlService
    {
        public Task<PresignedUploadResult> CreateUploadUrlAsync(PresignedUploadRequest request, CancellationToken cancellationToken)
        {
            if (request.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
            {
                throw new ArgumentException("Unsupported content type.", nameof(request));
            }

            if (request.FileSizeBytes > 10 * 1024 * 1024)
            {
                throw new ArgumentException("File too large.", nameof(request));
            }

            return Task.FromResult(new PresignedUploadResult(
                Guid.NewGuid(),
                "users/1/media/1/file.jpg",
                new Uri("https://example.com/upload"),
                DateTime.UtcNow.AddMinutes(5),
                new Dictionary<string, string>()));
        }

        public Task<PresignedViewResult> CreateViewUrlAsync(Guid mediaAssetId, Guid ownerUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PresignedViewResult(new Uri("https://example.com/view"), DateTime.UtcNow.AddMinutes(5)));
        }

        public Task DeleteMediaAssetAsync(Guid mediaAssetId, Guid ownerUserId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
