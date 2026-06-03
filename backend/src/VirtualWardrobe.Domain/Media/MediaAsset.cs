using VirtualWardrobe.Domain.Common;

namespace VirtualWardrobe.Domain.Media;

public sealed class MediaAsset : Entity<MediaAssetId>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private MediaAsset(
        MediaAssetId id,
        UserId ownerUserId,
        string storageKey,
        string contentType,
        int fileSizeBytes,
        string visibility,
        DateTime createdAtUtc)
        : base(id)
    {
        OwnerUserId = ownerUserId;
        StorageKey = storageKey;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        Visibility = visibility;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public UserId OwnerUserId { get; }

    public string StorageKey { get; }

    public string ContentType { get; }

    public int FileSizeBytes { get; }

    public string Visibility { get; }

    public static MediaAsset Create(
        MediaAssetId id,
        UserId ownerUserId,
        string storageKey,
        string contentType,
        int fileSizeBytes,
        DateTime? createdAtUtc = null)
    {
        if (ownerUserId.Value == Guid.Empty)
        {
            throw new ArgumentException("Owner user is required.", nameof(ownerUserId));
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ArgumentException("Unsupported media content type.", nameof(contentType));
        }

        if (fileSizeBytes <= 0 || fileSizeBytes > 10 * 1024 * 1024)
        {
            throw new ArgumentException("File size must be between 1 byte and 10 MB.", nameof(fileSizeBytes));
        }

        return new MediaAsset(
            id,
            ownerUserId,
            storageKey,
            contentType,
            fileSizeBytes,
            "PrivateOwnerOnly",
            createdAtUtc ?? DateTime.UtcNow);
    }

    public bool IsOwnedBy(UserId ownerUserId)
    {
        return OwnerUserId == ownerUserId;
    }
}
