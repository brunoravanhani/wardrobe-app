using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualWardrobe.Api.Infrastructure;
using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wardrobe;

namespace VirtualWardrobe.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/wardrobe-items")]
public sealed class WardrobeItemsController : ApiControllerBase
{
    private readonly CreateWardrobeItemCommand _createWardrobeItemCommand;

    public WardrobeItemsController(CreateWardrobeItemCommand createWardrobeItemCommand)
    {
        _createWardrobeItemCommand = createWardrobeItemCommand;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WardrobeItemResponse>>> ListAsync(
        [FromQuery] ClothingCategory? category,
        CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetRequiredUserId();
        var items = await _createWardrobeItemCommand.ListAsync(ownerUserId, category, cancellationToken);
        return Ok(items.Select(Map).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<WardrobeItemResponse>> CreateAsync(
        [FromBody] CreateWardrobeItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseCategory(request.Category, out var category))
        {
            return Problem(
                title: "Wardrobe request failed",
                detail: "Invalid category.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var ownerUserId = User.GetRequiredUserId();
        var result = await _createWardrobeItemCommand.CreateAsync(
            new CreateWardrobeItemInput(
                ownerUserId,
                category,
                request.Name,
                request.Size,
                request.Brand,
                request.Price,
                request.BodyImageAssetId,
                request.CareTagImageAssetId),
            cancellationToken);

        return ToActionResult(result, Map, StatusCodes.Status201Created, "Wardrobe request failed");
    }

    [HttpPatch("{itemId:guid}")]
    public async Task<ActionResult<WardrobeItemResponse>> UpdateAsync(
        Guid itemId,
        [FromBody] UpdateWardrobeItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseCategory(request.Category, out var category))
        {
            return Problem(
                title: "Wardrobe request failed",
                detail: "Invalid category.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var ownerUserId = User.GetRequiredUserId();
        var result = await _createWardrobeItemCommand.UpdateAsync(
            new UpdateWardrobeItemInput(
                itemId,
                ownerUserId,
                category,
                request.Name,
                request.Size,
                request.Brand,
                request.Price,
                request.BodyImageAssetId,
                request.CareTagImageAssetId),
            cancellationToken);

        return ToActionResult(result, Map, StatusCodes.Status200OK, "Wardrobe request failed");
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetRequiredUserId();
        var result = await _createWardrobeItemCommand.DeleteAsync(itemId, ownerUserId, cancellationToken);

        if (result.IsFailure)
            return ProblemFromError(result.Error, "Wardrobe request failed");

        return NoContent();
    }

    private static WardrobeItemResponse Map(WardrobeItem item)
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
}

public sealed record CreateWardrobeItemRequest(
    string Category,
    string Name,
    string Size,
    string? Brand,
    decimal? Price,
    Guid? BodyImageAssetId,
    Guid? CareTagImageAssetId);

public sealed record UpdateWardrobeItemRequest(
    string Category,
    string Name,
    string Size,
    string? Brand,
    decimal? Price,
    Guid? BodyImageAssetId,
    Guid? CareTagImageAssetId);

public sealed record WardrobeItemResponse(
    Guid Id,
    ClothingCategory Category,
    string Name,
    string? Brand,
    string Size,
    decimal? Price,
    Guid? BodyImageAssetId,
    Guid? CareTagImageAssetId);
