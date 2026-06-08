using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wardrobe;

namespace VirtualWardrobe.Application.Wardrobe;

public sealed record CreateWardrobeItemInput(
    Guid OwnerUserId,
    ClothingCategory Category,
    string Name,
    string Size,
    string? Brand,
    decimal? Price,
    Guid? BodyImageAssetId,
    Guid? CareTagImageAssetId
);

public sealed record UpdateWardrobeItemInput(
    Guid ItemId,
    Guid OwnerUserId,
    ClothingCategory Category,
    string Name,
    string Size,
    string? Brand,
    decimal? Price,
    Guid? BodyImageAssetId,
    Guid? CareTagImageAssetId
);

public sealed class CreateWardrobeItemCommand
{
    private readonly IWardrobeItemRepository _wardrobeItemRepository;
    private readonly IMediaAssetRepository _mediaAssetRepository;

    public CreateWardrobeItemCommand(
        IWardrobeItemRepository wardrobeItemRepository,
        IMediaAssetRepository mediaAssetRepository)
    {
        _wardrobeItemRepository = wardrobeItemRepository;
        _mediaAssetRepository = mediaAssetRepository;
    }

    public async Task<Result<WardrobeItem>> CreateAsync(CreateWardrobeItemInput input, CancellationToken cancellationToken)
    {
        var ownerUserId = new UserId(input.OwnerUserId);
        var mediaValidation = await ValidateMediaOwnershipAsync(
            ownerUserId,
            input.BodyImageAssetId,
            input.CareTagImageAssetId,
            cancellationToken);

        if (mediaValidation.IsFailure)
        {
            return Result.Failure<WardrobeItem>(mediaValidation.Error);
        }

        try
        {
            var item = WardrobeItem.Create(
                WardrobeItemId.New(),
                ownerUserId,
                input.Category,
                input.Name,
                input.Size,
                input.Brand,
                input.Price,
                input.BodyImageAssetId.HasValue ? new MediaAssetId(input.BodyImageAssetId.Value) : null,
                input.CareTagImageAssetId.HasValue ? new MediaAssetId(input.CareTagImageAssetId.Value) : null);

            await _wardrobeItemRepository.AddAsync(item, cancellationToken);
            await _wardrobeItemRepository.SaveChangesAsync(cancellationToken);

            return Result.Success(item);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<WardrobeItem>(ResultError.Validation(exception.Message));
        }
    }

    public async Task<Result<WardrobeItem>> UpdateAsync(UpdateWardrobeItemInput input, CancellationToken cancellationToken)
    {
        var ownerUserId = new UserId(input.OwnerUserId);
        var itemId = new WardrobeItemId(input.ItemId);

        var item = await _wardrobeItemRepository.GetByIdAsync(itemId, ownerUserId, cancellationToken);
        if (item is null)
        {
            return Result.Failure<WardrobeItem>(ResultError.NotFound("Wardrobe item was not found."));
        }

        var mediaValidation = await ValidateMediaOwnershipAsync(
            ownerUserId,
            input.BodyImageAssetId,
            input.CareTagImageAssetId,
            cancellationToken);

        if (mediaValidation.IsFailure)
        {
            return Result.Failure<WardrobeItem>(mediaValidation.Error);
        }

        try
        {
            item.Update(
                input.Category,
                input.Name,
                input.Size,
                input.Brand,
                input.Price,
                input.BodyImageAssetId.HasValue ? new MediaAssetId(input.BodyImageAssetId.Value) : null,
                input.CareTagImageAssetId.HasValue ? new MediaAssetId(input.CareTagImageAssetId.Value) : null);

            await _wardrobeItemRepository.UpdateAsync(item, cancellationToken);
            await _wardrobeItemRepository.SaveChangesAsync(cancellationToken);
            return Result.Success(item);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<WardrobeItem>(ResultError.Validation(exception.Message));
        }
    }

    public async Task<Result> DeleteAsync(Guid itemId, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var item = await _wardrobeItemRepository.GetByIdAsync(new WardrobeItemId(itemId), new UserId(ownerUserId), cancellationToken);
        if (item is null)
        {
            return Result.Failure(ResultError.NotFound("Wardrobe item was not found."));
        }

        await _wardrobeItemRepository.RemoveAsync(item, cancellationToken);
        await _wardrobeItemRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public Task<IReadOnlyList<WardrobeItem>> ListAsync(Guid ownerUserId, ClothingCategory? category, CancellationToken cancellationToken)
    {
        return _wardrobeItemRepository.ListAsync(new UserId(ownerUserId), category, cancellationToken);
    }

    private async Task<Result> ValidateMediaOwnershipAsync(
        UserId ownerUserId,
        Guid? bodyImageAssetId,
        Guid? careTagImageAssetId,
        CancellationToken cancellationToken)
    {
        if (bodyImageAssetId.HasValue)
        {
            var ownsBody = await _mediaAssetRepository.ExistsOwnedByAsync(
                new MediaAssetId(bodyImageAssetId.Value),
                ownerUserId,
                cancellationToken);

            if (!ownsBody)
            {
                return Result.Failure(ResultError.Forbidden("Body image must belong to the authenticated user."));
            }
        }

        if (careTagImageAssetId.HasValue)
        {
            var ownsCareTag = await _mediaAssetRepository.ExistsOwnedByAsync(
                new MediaAssetId(careTagImageAssetId.Value),
                ownerUserId,
                cancellationToken);

            if (!ownsCareTag)
            {
                return Result.Failure(ResultError.Forbidden("Care tag image must belong to the authenticated user."));
            }
        }

        return Result.Success();
    }
}
