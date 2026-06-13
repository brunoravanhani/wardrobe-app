using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Domain.Common;
using VirtualWardrobe.Domain.Wardrobe;
using VirtualWardrobe.Domain.Wishlist;

namespace VirtualWardrobe.Application.Wishlist;

public sealed record ConvertWishlistItemInput(
    Guid ItemId,
    Guid OwnerUserId,
    string? Name,
    ClothingCategory? Category,
    string Size,
    string? Brand,
    decimal? Price,
    Guid? BodyImageAssetId,
    Guid? CareTagImageAssetId
);

public sealed class ConvertWishlistItemCommand
{
    private readonly IWishlistItemRepository _wishlistItemRepository;
    private readonly IWardrobeItemRepository _wardrobeItemRepository;
    private readonly IMediaAssetRepository _mediaAssetRepository;

    public ConvertWishlistItemCommand(
        IWishlistItemRepository wishlistItemRepository,
        IWardrobeItemRepository wardrobeItemRepository,
        IMediaAssetRepository mediaAssetRepository)
    {
        _wishlistItemRepository = wishlistItemRepository;
        _wardrobeItemRepository = wardrobeItemRepository;
        _mediaAssetRepository = mediaAssetRepository;
    }

    public async Task<Result<WardrobeItem>> CombinedConvertAsync(ConvertWishlistItemInput input, CancellationToken cancellationToken)
    {
        var ownerUserId = new UserId(input.OwnerUserId);
        var wishlistItem = await _wishlistItemRepository.GetByIdAsync(new WishlistItemId(input.ItemId), ownerUserId, cancellationToken);

        if (wishlistItem is null)
        {
            return Result.Failure<WardrobeItem>(ResultError.NotFound("Wishlist item was not found."));
        }

        if (wishlistItem.ConvertedWardrobeItemId.HasValue)
        {
            var existing = await _wardrobeItemRepository.GetByIdAsync(
                new WardrobeItemId(wishlistItem.ConvertedWardrobeItemId.Value),
                ownerUserId,
                cancellationToken);

            if (existing is not null)
            {
                return Result.Success(existing);
            }
        }

        if (string.IsNullOrWhiteSpace(input.Size))
        {
            return Result.Failure<WardrobeItem>(ResultError.Validation("Size is required to convert wishlist item into wardrobe item."));
        }

        var bodyImageAssetId = input.BodyImageAssetId ?? wishlistItem.InspirationImageAssetId?.Value;
        var mediaValidation = await ValidateMediaOwnershipAsync(
            ownerUserId,
            bodyImageAssetId,
            input.CareTagImageAssetId,
            cancellationToken);

        if (mediaValidation.IsFailure)
        {
            return Result.Failure<WardrobeItem>(mediaValidation.Error);
        }

        try
        {
            if (wishlistItem.Status == WishlistItemStatus.Active)
            {
                wishlistItem.ConvertToWardrobe();
            }

            var wardrobeItem = WardrobeItem.Create(
                WardrobeItemId.New(),
                ownerUserId,
                input.Category ?? wishlistItem.Category,
                string.IsNullOrWhiteSpace(input.Name) ? wishlistItem.Name : input.Name,
                input.Size,
                input.Brand ?? wishlistItem.Brand,
                input.Price ?? wishlistItem.TargetPrice,
                bodyImageAssetId.HasValue ? new MediaAssetId(bodyImageAssetId.Value) : null,
                input.CareTagImageAssetId.HasValue ? new MediaAssetId(input.CareTagImageAssetId.Value) : null);

            await _wardrobeItemRepository.AddAsync(wardrobeItem, cancellationToken);

            wishlistItem.MarkAsConverted(wardrobeItem.Id.Value);
            await _wishlistItemRepository.UpdateAsync(wishlistItem, cancellationToken);

            await _wardrobeItemRepository.SaveChangesAsync(cancellationToken);

            return Result.Success(wardrobeItem);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<WardrobeItem>(ResultError.Validation(exception.Message));
        }
    }

    public Task<IReadOnlyList<WishlistItem>> ListAsync(Guid ownerUserId, bool includePurchased, CancellationToken cancellationToken)
    {
        return _wishlistItemRepository.ListAsync(new UserId(ownerUserId), includePurchased, cancellationToken);
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
