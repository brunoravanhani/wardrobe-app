namespace VirtualWardrobe.Infrastructure.Persistence.Entities;

public sealed class MediaAssetRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public UserRecord User { get; set; } = default!;

    public string StorageKey { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public int FileSizeBytes { get; set; }

    public string Visibility { get; set; } = "PrivateOwnerOnly";

    public DateTime CreatedAtUtc { get; set; }
}