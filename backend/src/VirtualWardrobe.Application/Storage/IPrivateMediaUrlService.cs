namespace VirtualWardrobe.Application.Storage;

public sealed record PresignedUploadRequest(
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Purpose,
    Guid OwnerUserId
);

public sealed record PresignedUploadResult(
    Guid MediaAssetId,
    string StorageKey,
    Uri UploadUrl,
    DateTime ExpiresAtUtc,
    IReadOnlyDictionary<string, string> RequiredHeaders
);

public sealed record PresignedViewResult(Uri ViewUrl, DateTime ExpiresAtUtc);

public interface IPrivateMediaUrlService
{
    Task<PresignedUploadResult> CreateUploadUrlAsync(PresignedUploadRequest request, CancellationToken cancellationToken);

    Task<PresignedViewResult> CreateViewUrlAsync(Guid mediaAssetId, Guid ownerUserId, CancellationToken cancellationToken);
}