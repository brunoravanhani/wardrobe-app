using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualWardrobe.Api.Infrastructure;
using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Application.Wishlist;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wishlist;

namespace VirtualWardrobe.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/wishlist-items")]
public sealed class WishlistItemsController : ControllerBase
{
    private readonly CreateWishlistItemCommand _createWishlistItemCommand;

    public WishlistItemsController(CreateWishlistItemCommand createWishlistItemCommand)
    {
        _createWishlistItemCommand = createWishlistItemCommand;
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
        var ownerUserId = User.GetRequiredUserId();

        var result = await _createWishlistItemCommand.CreateAsync(
            new CreateWishlistItemInput(
                ownerUserId,
                request.Category,
                request.Name,
                request.Brand,
                request.TargetPrice,
                request.InspirationImageAssetId,
                request.Links ?? []),
            cancellationToken);

        return ToActionResult(result, StatusCodes.Status201Created);
    }

    [HttpPatch("{itemId:guid}")]
    public async Task<ActionResult<WishlistItemResponse>> UpdateAsync(
        Guid itemId,
        [FromBody] UpdateWishlistItemRequest request,
        CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetRequiredUserId();

        var result = await _createWishlistItemCommand.UpdateAsync(
            new UpdateWishlistItemInput(
                itemId,
                ownerUserId,
                request.Category,
                request.Name,
                request.Brand,
                request.TargetPrice,
                request.InspirationImageAssetId,
                request.Links ?? []),
            cancellationToken);

        return ToActionResult(result, StatusCodes.Status200OK);
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetRequiredUserId();
        var result = await _createWishlistItemCommand.DeleteAsync(itemId, ownerUserId, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = result.Error.Code switch
            {
                "forbidden" => StatusCodes.Status403Forbidden,
                "not_found" => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };

            return Problem(title: "Wishlist request failed", detail: result.Error.Message, statusCode: statusCode);
        }

        return NoContent();
    }

    private ActionResult<WishlistItemResponse> ToActionResult(Result<WishlistItem> result, int successStatusCode)
    {
        if (result.IsFailure)
        {
            return ProblemFromError(result.Error);
        }

        var response = Map(result.Value);
        if (successStatusCode == StatusCodes.Status201Created)
        {
            return CreatedAtAction(nameof(ListAsync), response);
        }

        return Ok(response);
    }

    private ActionResult<WishlistItemResponse> ProblemFromError(ResultError error)
    {
        var statusCode = error.Code switch
        {
            "forbidden" => StatusCodes.Status403Forbidden,
            "not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(title: "Wishlist request failed", detail: error.Message, statusCode: statusCode);
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
            item.ExternalLinks.Select(x => x.Url).ToArray(),
            item.Status,
            item.PurchasedAtUtc,
            item.ConvertedWardrobeItemId);
    }
}

public sealed record CreateWishlistItemRequest(
    ClothingCategory Category,
    string Name,
    string? Brand,
    decimal TargetPrice,
    Guid? InspirationImageAssetId,
    string[]? Links);

public sealed record UpdateWishlistItemRequest(
    ClothingCategory Category,
    string Name,
    string? Brand,
    decimal TargetPrice,
    Guid? InspirationImageAssetId,
    string[]? Links);

public sealed record WishlistItemResponse(
    Guid Id,
    ClothingCategory Category,
    string Name,
    string? Brand,
    decimal TargetPrice,
    Guid? InspirationImageAssetId,
    string[] Links,
    WishlistItemStatus Status,
    DateTime? PurchasedAtUtc,
    Guid? ConvertedWardrobeItemId);
