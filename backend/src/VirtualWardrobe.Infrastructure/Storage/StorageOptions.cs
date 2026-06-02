namespace VirtualWardrobe.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "AWS:S3";

    public string BucketName { get; set; } = string.Empty;

    public int UploadUrlExpirationMinutes { get; set; } = 10;

    public int ViewUrlExpirationMinutes { get; set; } = 5;
}