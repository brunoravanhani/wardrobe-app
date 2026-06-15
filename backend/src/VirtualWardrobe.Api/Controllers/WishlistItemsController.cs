using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualWardrobe.Api.Infrastructure;
using VirtualWardrobe.Api.Observability;
using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Application.Wishlist;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wardrobe;
using VirtualWardrobe.Domain.Wishlist;

namespace VirtualWardrobe.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/wishlist-items")]
public sealed partial class WishlistItemsController : ApiControllerBase
{
    private readonly CreateWishlistItemCommand _createWishlistItemCommand;
    private readonly ConvertWishlistItemCommand _convertWishlistItemCommand;
    private readonly ILogger<WishlistItemsController> _logger;

    public WishlistItemsController(
        CreateWishlistItemCommand createWishlistItemCommand,
        ConvertWishlistItemCommand convertWishlistItemCommand,
        ILogger<WishlistItemsController> logger)
    {
        _createWishlistItemCommand = createWishlistItemCommand;
        _convertWishlistItemCommand = convertWishlistItemCommand;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WishlistItemResponse>>> ListAsync(
        [FromQuery] bool includePurchased = false,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = User.GetRequiredUserId();
        var items = await _createWishlistItemCommand.ListAsync(ownerUserId, includePurchased, cancellationToken);
        return Ok(items.Select(Map).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<WishlistItemResponse>> CreateAsync(
        [FromBody] CreateWishlistItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseCategory(request.Category, out var category))
        {
            return Problem(
                title: "Wishlist request failed",
                detail: "Invalid category.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var ownerUserId = User.GetRequiredUserId();

        var result = await _createWishlistItemCommand.CreateAsync(
            new CreateWishlistItemInput(
                ownerUserId,
                category,
                request.Name,
                request.Brand,
                request.TargetPrice,
                request.InspirationImageAssetId,
                (request.Links ?? []).Select(x => new WishlistLinkInput(x.Url, x.Label)).ToArray()),
            cancellationToken);

        return ToActionResult(result, Map, StatusCodes.Status201Created, "Wishlist request failed");
    }

    [HttpPatch("{itemId:guid}")]
    public async Task<ActionResult<WishlistItemResponse>> UpdateAsync(
        Guid itemId,
        [FromBody] UpdateWishlistItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseCategory(request.Category, out var category))
        {
            return Problem(
                title: "Wishlist request failed",
                detail: "Invalid category.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var ownerUserId = User.GetRequiredUserId();

        var result = await _createWishlistItemCommand.UpdateAsync(
            new UpdateWishlistItemInput(
                itemId,
                ownerUserId,
                category,
                request.Name,
                request.Brand,
                request.TargetPrice,
                request.InspirationImageAssetId,
                (request.Links ?? []).Select(x => new WishlistLinkInput(x.Url, x.Label)).ToArray()),
            cancellationToken);

        return ToActionResult(result, Map, StatusCodes.Status200OK, "Wishlist request failed");
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetRequiredUserId();
        var result = await _createWishlistItemCommand.DeleteAsync(itemId, ownerUserId, cancellationToken);

        if (result.IsFailure)
            return ProblemFromError(result.Error, "Wishlist request failed");

        return NoContent();
    }

    [HttpPost("{itemId:guid}/convert")]
    public async Task<ActionResult<WishlistConversionResponse>> ConvertToWardrobeAsync(
        Guid itemId,
        [FromBody] ConvertWishlistItemRequest request,
        CancellationToken cancellationToken)
    {
        ClothingCategory? category = null;
        if (request.Category is not null)
        {
            if (!TryParseCategory(request.Category, out var parsedCategory))
            {
                return Problem(
                    title: "Wishlist request failed",
                    detail: "Invalid category.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            category = parsedCategory;
        }

        TelemetryConfig.WishlistConversionTotal.Add(1);
        var ownerUserId = User.GetRequiredUserId();
        Log.ConversionInitiated(_logger, itemId, ownerUserId);

        var result = await _convertWishlistItemCommand.CombinedConvertAsync(
            new ConvertWishlistItemInput(
                itemId,
                ownerUserId,
                request.Name,
                category,
                request.Size,
                request.Brand,
                request.Price,
                request.BodyImageAssetId,
                request.CareTagImageAssetId),
            cancellationToken);

        if (result.IsFailure)
        {
            TelemetryConfig.WishlistConversionFailures.Add(1);
            Log.ConversionFailed(_logger, itemId, result.Error.Message);
            return ProblemFromError(result.Error, "Wishlist request failed");
        }

        TelemetryConfig.WishlistConversionSuccesses.Add(1);
        Log.ConversionSucceeded(_logger, itemId, result.Value.Id.Value);

        return Ok(new WishlistConversionResponse(itemId, MapWardrobe(result.Value)));
    }

    private static WishlistItemResponse Map(WishlistItem item)
    {
        return new WishlistItemResponse(
            item.Id.Value,
            item.Category,
            item.Name,
            item.Brand,
            item.TargetPrice,
            item.InspirationImageAssetId?.Value,
            item.ExternalLinks.Select(x => new WishlistLinkPayload(x.Url, x.Label)).ToArray(),
            item.Status,
            item.PurchasedAtUtc,
            item.ConvertedWardrobeItemId);
    }

    private static WardrobeItemResponse MapWardrobe(WardrobeItem item)
    {
        return new WardrobeItemResponse(
            item.Id.Value,
            item.Category,
            item.Name,
            item.Brand,
            item.Size,
            item.Price,
            item.BodyImageAssetId?.Value,
            item.CareTagImageAssetId?.Value);
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Wishlist conversion initiated for item {WishlistItemId} by user {UserId}")]
        internal static partial void ConversionInitiated(ILogger logger, Guid wishlistItemId, Guid userId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Wishlist conversion failed for item {WishlistItemId}: {Error}")]
        internal static partial void ConversionFailed(ILogger logger, Guid wishlistItemId, string error);

        [LoggerMessage(Level = LogLevel.Information, Message = "Wishlist item {WishlistItemId} converted to wardrobe item {WardrobeItemId}")]
        internal static partial void ConversionSucceeded(ILogger logger, Guid wishlistItemId, Guid wardrobeItemId);
    }
}

public sealed record WishlistLinkPayload(string Url, string? Label);

public sealed record CreateWishlistItemRequest(
    string Category,
    string Name,
    string? Brand,
    decimal TargetPrice,
    Guid? InspirationImageAssetId,
    WishlistLinkPayload[]? Links);

public sealed record UpdateWishlistItemRequest(
    string Category,
    string Name,
    string? Brand,
    decimal TargetPrice,
    Guid? InspirationImageAssetId,
    WishlistLinkPayload[]? Links);

public sealed record ConvertWishlistItemRequest(
    string? Name,
    string? Category,
    string Size,
    string? Brand,
    decimal? Price,
    Guid? BodyImageAssetId,
    Guid? CareTagImageAssetId);

public sealed record WishlistItemResponse(
    Guid Id,
    ClothingCategory Category,
    string Name,
    string? Brand,
    decimal TargetPrice,
    Guid? InspirationImageAssetId,
    WishlistLinkPayload[] Links,
    WishlistItemStatus Status,
    DateTime? PurchasedAtUtc,
    Guid? ConvertedWardrobeItemId);

public sealed record WishlistConversionResponse(Guid WishlistItemId, WardrobeItemResponse WardrobeItem);
