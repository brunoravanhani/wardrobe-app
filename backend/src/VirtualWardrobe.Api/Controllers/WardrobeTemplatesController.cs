using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualWardrobe.Api.Infrastructure;
using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Application.Templates;
using VirtualWardrobe.Domain.Templates;
using VirtualWardrobe.Domain.Wishlist;

namespace VirtualWardrobe.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/wardrobe-templates")]
public sealed class WardrobeTemplatesController : ApiControllerBase
{
    private readonly GetTemplatesQuery _getTemplatesQuery;
    private readonly GetUserSlotsQuery _getUserSlotsQuery;
    private readonly SelectTemplateCommand _selectTemplateCommand;
    private readonly LinkSlotToWishlistCommand _linkSlotToWishlistCommand;

    public WardrobeTemplatesController(
        GetTemplatesQuery getTemplatesQuery,
        GetUserSlotsQuery getUserSlotsQuery,
        SelectTemplateCommand selectTemplateCommand,
        LinkSlotToWishlistCommand linkSlotToWishlistCommand)
    {
        _getTemplatesQuery = getTemplatesQuery;
        _getUserSlotsQuery = getUserSlotsQuery;
        _selectTemplateCommand = selectTemplateCommand;
        _linkSlotToWishlistCommand = linkSlotToWishlistCommand;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WardrobeTemplateResponse>>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        var templates = await _getTemplatesQuery.ExecuteAsync(cancellationToken);
        return Ok(templates.Select(MapTemplate).ToArray());
    }

    [HttpGet("slots")]
    public async Task<ActionResult<UserSlotsResponse>> GetUserSlotsAsync(CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetRequiredUserId();
        var (activeTemplateId, slots) = await _getUserSlotsQuery.ExecuteAsync(ownerUserId, cancellationToken);
        return Ok(new UserSlotsResponse(activeTemplateId, slots.Select(MapSlot).ToArray()));
    }

    [HttpPost("{templateId:guid}/select")]
    public async Task<IActionResult> SelectTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetRequiredUserId();
        var result = await _selectTemplateCommand.ExecuteAsync(
            new SelectTemplateInput(ownerUserId, templateId),
            cancellationToken);

        if (result.IsFailure)
            return ProblemFromError(result.Error, "Template selection failed");

        return NoContent();
    }

    [HttpPost("slots/{slotId:guid}/link-wishlist")]
    public async Task<ActionResult<WishlistItemResponse>> LinkSlotToWishlistAsync(
        Guid slotId,
        [FromBody] LinkSlotToWishlistRequest request,
        CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetRequiredUserId();
        var result = await _linkSlotToWishlistCommand.ExecuteAsync(
            new LinkSlotToWishlistInput(slotId, ownerUserId, request.Name, request.Brand, request.TargetPrice),
            cancellationToken);

        if (result.IsFailure)
            return ProblemFromError(result.Error, "Link slot to wishlist failed");

        return CreatedAtAction(nameof(GetUserSlotsAsync), MapWishlistItem(result.Value));
    }

    private static WardrobeTemplateResponse MapTemplate(WardrobeTemplate template)
    {
        return new WardrobeTemplateResponse(
            template.Id.Value,
            template.Name,
            template.SlotDefinitions.Select(d => new TemplateSlotDefinitionResponse(d.Id.Value, d.Category.ToString(), d.Quantity)).ToArray());
    }

    private static TemplateSlotResponse MapSlot(TemplateSlot slot)
    {
        return new TemplateSlotResponse(
            slot.Id.Value,
            slot.TemplateId.Value,
            slot.Category.ToString(),
            slot.WardrobeItemId?.Value,
            slot.WishlistItemId?.Value,
            slot.IsFulfilled,
            slot.FulfilledAtUtc,
            slot.CreatedAtUtc);
    }

    private static WishlistItemResponse MapWishlistItem(WishlistItem item)
    {
        return new WishlistItemResponse(
            item.Id.Value,
            item.Category,
            item.Name,
            item.Brand,
            item.TargetPrice,
            null,
            [],
            item.Status,
            item.PurchasedAtUtc,
            item.ConvertedWardrobeItemId);
    }
}

public sealed record WardrobeTemplateResponse(
    Guid Id,
    string Name,
    IReadOnlyList<TemplateSlotDefinitionResponse> SlotDefinitions);

public sealed record TemplateSlotDefinitionResponse(Guid Id, string Category, int Quantity);

public sealed record TemplateSlotResponse(
    Guid Id,
    Guid TemplateId,
    string Category,
    Guid? WardrobeItemId,
    Guid? WishlistItemId,
    bool IsFulfilled,
    DateTime? FulfilledAtUtc,
    DateTime CreatedAtUtc);

public sealed record UserSlotsResponse(
    Guid? ActiveTemplateId,
    IReadOnlyList<TemplateSlotResponse> Slots);

public sealed record LinkSlotToWishlistRequest(
    string Name,
    string? Brand,
    decimal TargetPrice);
