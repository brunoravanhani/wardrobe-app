using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VirtualWardrobe.Application.Storage;
using VirtualWardrobe.Infrastructure.Persistence;
using VirtualWardrobe.Infrastructure.Persistence.Entities;

namespace VirtualWardrobe.Infrastructure.Storage;

public sealed class S3PresignedUrlService : IPrivateMediaUrlService
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private readonly IAmazonS3 _s3Client;
    private readonly VirtualWardrobeDbContext _dbContext;
    private readonly StorageOptions _options;

    public S3PresignedUrlService(
        IAmazonS3 s3Client,
        VirtualWardrobeDbContext dbContext,
        IOptions<StorageOptions> options)
    {
        _s3Client = s3Client;
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<PresignedUploadResult> CreateUploadUrlAsync(
        PresignedUploadRequest request,
        CancellationToken cancellationToken)
    {
        ValidateUploadRequest(request);

        var mediaAssetId = Guid.NewGuid();
        var storageKey = $"users/{request.OwnerUserId}/media/{mediaAssetId}/{request.FileName}";
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.UploadUrlExpirationMinutes);

        var presignedRequest = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAtUtc,
            ContentType = request.ContentType
        };

        var uploadUrl = await _s3Client.GetPreSignedURLAsync(presignedRequest);

        var mediaAsset = new MediaAssetRecord
        {
            Id = mediaAssetId,
            UserId = request.OwnerUserId,
            StorageKey = storageKey,
            ContentType = request.ContentType,
            FileSizeBytes = checked((int)request.FileSizeBytes),
            Visibility = "PrivateOwnerOnly",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.MediaAssets.AddAsync(mediaAsset, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PresignedUploadResult(
            mediaAsset.Id,
            storageKey,
            new Uri(uploadUrl),
            expiresAtUtc,
            new Dictionary<string, string>
            {
                ["Content-Type"] = request.ContentType
            });
    }

    public async Task<PresignedViewResult> CreateViewUrlAsync(Guid mediaAssetId, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var mediaAsset = await _dbContext.MediaAssets.SingleOrDefaultAsync(
            x => x.Id == mediaAssetId && x.UserId == ownerUserId,
            cancellationToken);

        if (mediaAsset is null)
        {
            throw new KeyNotFoundException("Media asset not found for owner.");
        }

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ViewUrlExpirationMinutes);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = mediaAsset.StorageKey,
            Verb = HttpVerb.GET,
            Expires = expiresAtUtc
        };

        var viewUrl = await _s3Client.GetPreSignedURLAsync(request);
        return new PresignedViewResult(new Uri(viewUrl), expiresAtUtc);
    }

    public async Task DeleteMediaAssetAsync(Guid mediaAssetId, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var mediaAsset = await _dbContext.MediaAssets.SingleOrDefaultAsync(
            x => x.Id == mediaAssetId && x.UserId == ownerUserId,
            cancellationToken);

        if (mediaAsset is null)
        {
            return;
        }

        await _s3Client.DeleteObjectAsync(_options.BucketName, mediaAsset.StorageKey, cancellationToken);

        _dbContext.MediaAssets.Remove(mediaAsset);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateUploadRequest(PresignedUploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("FileName is required.", nameof(request));
        }

        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            throw new ArgumentException("Unsupported content type.", nameof(request));
        }

        if (request.FileSizeBytes <= 0 || request.FileSizeBytes > 10 * 1024 * 1024)
        {
            throw new ArgumentException("File size must be between 1 byte and 10 MB.", nameof(request));
        }
    }
}