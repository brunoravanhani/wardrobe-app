using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Application.Storage;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wishlist;

namespace VirtualWardrobe.Application.Wishlist;

public sealed record CreateWishlistItemInput(
    Guid OwnerUserId,
    ClothingCategory Category,
    string Name,
    string? Brand,
    decimal TargetPrice,
    Guid? InspirationImageAssetId,
    IReadOnlyList<string> Links
);

public sealed record UpdateWishlistItemInput(
    Guid ItemId,
    Guid OwnerUserId,
    ClothingCategory Category,
    string Name,
    string? Brand,
    decimal TargetPrice,
    Guid? InspirationImageAssetId,
    IReadOnlyList<string> Links
);

public sealed class CreateWishlistItemCommand
{
    private readonly IWishlistItemRepository _wishlistItemRepository;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IPrivateMediaUrlService _mediaUrlService;

    public CreateWishlistItemCommand(
        IWishlistItemRepository wishlistItemRepository,
        IMediaAssetRepository mediaAssetRepository,
        IPrivateMediaUrlService mediaUrlService)
    {
        _wishlistItemRepository = wishlistItemRepository;
        _mediaAssetRepository = mediaAssetRepository;
        _mediaUrlService = mediaUrlService;
    }

    public async Task<Result<WishlistItem>> CreateAsync(CreateWishlistItemInput input, CancellationToken cancellationToken)
    {
        var ownerUserId = new UserId(input.OwnerUserId);
        var mediaValidation = await ValidateMediaOwnershipAsync(ownerUserId, input.InspirationImageAssetId, cancellationToken);

        if (mediaValidation.IsFailure)
        {
            return Result.Failure<WishlistItem>(mediaValidation.Error);
        }

        try
        {
            var item = WishlistItem.Create(
                WishlistItemId.New(),
                ownerUserId,
                input.Category,
                input.Name,
                input.Brand,
                input.TargetPrice,
                input.InspirationImageAssetId.HasValue ? new MediaAssetId(input.InspirationImageAssetId.Value) : null,
                input.Links);

            await _wishlistItemRepository.AddAsync(item, cancellationToken);
            await _wishlistItemRepository.SaveChangesAsync(cancellationToken);

            return Result.Success(item);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<WishlistItem>(ResultError.Validation(exception.Message));
        }
    }

    public async Task<Result<WishlistItem>> UpdateAsync(UpdateWishlistItemInput input, CancellationToken cancellationToken)
    {
        var ownerUserId = new UserId(input.OwnerUserId);
        var itemId = new WishlistItemId(input.ItemId);

        var item = await _wishlistItemRepository.GetByIdAsync(itemId, ownerUserId, cancellationToken);
        if (item is null)
        {
            return Result.Failure<WishlistItem>(ResultError.NotFound("Wishlist item was not found."));
        }

        var oldInspirationImageId = item.InspirationImageAssetId?.Value;

        var mediaValidation = await ValidateMediaOwnershipAsync(ownerUserId, input.InspirationImageAssetId, cancellationToken);

        if (mediaValidation.IsFailure)
        {
            return Result.Failure<WishlistItem>(mediaValidation.Error);
        }

        try
        {
            item.Update(
                input.Category,
                input.Name,
                input.Brand,
                input.TargetPrice,
                input.InspirationImageAssetId.HasValue ? new MediaAssetId(input.InspirationImageAssetId.Value) : null,
                input.Links);

            await _wishlistItemRepository.UpdateAsync(item, cancellationToken);
            await _wishlistItemRepository.SaveChangesAsync(cancellationToken);

            if (oldInspirationImageId.HasValue && oldInspirationImageId != input.InspirationImageAssetId)
            {
                await _mediaUrlService.DeleteMediaAssetAsync(oldInspirationImageId.Value, input.OwnerUserId, cancellationToken);
            }

            return Result.Success(item);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<WishlistItem>(ResultError.Validation(exception.Message));
        }
    }

    public async Task<Result> DeleteAsync(Guid itemId, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var item = await _wishlistItemRepository.GetByIdAsync(new WishlistItemId(itemId), new UserId(ownerUserId), cancellationToken);
        if (item is null)
        {
            return Result.Failure(ResultError.NotFound("Wishlist item was not found."));
        }

        var inspirationImageId = item.InspirationImageAssetId?.Value;

        await _wishlistItemRepository.RemoveAsync(item, cancellationToken);
        await _wishlistItemRepository.SaveChangesAsync(cancellationToken);

        if (inspirationImageId.HasValue)
        {
            await _mediaUrlService.DeleteMediaAssetAsync(inspirationImageId.Value, ownerUserId, cancellationToken);
        }

        return Result.Success();
    }

    public Task<IReadOnlyList<WishlistItem>> ListAsync(Guid ownerUserId, bool includePurchased, CancellationToken cancellationToken)
    {
        return _wishlistItemRepository.ListAsync(new UserId(ownerUserId), includePurchased, cancellationToken);
    }

    private async Task<Result> ValidateMediaOwnershipAsync(UserId ownerUserId, Guid? inspirationImageAssetId, CancellationToken cancellationToken)
    {
        if (!inspirationImageAssetId.HasValue)
        {
            return Result.Success();
        }

        var ownsImage = await _mediaAssetRepository.ExistsOwnedByAsync(
            new MediaAssetId(inspirationImageAssetId.Value),
            ownerUserId,
            cancellationToken);

        return ownsImage
            ? Result.Success()
            : Result.Failure(ResultError.Forbidden("Inspiration image must belong to the authenticated user."));
    }
}
