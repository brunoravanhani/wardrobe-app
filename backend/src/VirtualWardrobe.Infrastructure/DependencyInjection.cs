using Amazon.S3;
using Amazon;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtualWardrobe.Application.Auth;
using VirtualWardrobe.Application.Storage;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Application.Wishlist;
using VirtualWardrobe.Infrastructure.Auth;
using VirtualWardrobe.Infrastructure.Persistence.Configurations;
using VirtualWardrobe.Infrastructure.Persistence;
using VirtualWardrobe.Infrastructure.Storage;

namespace VirtualWardrobe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddVirtualWardrobePersistence(configuration);

        services.Configure<GoogleAuthOptions>(options =>
        {
            options.ClientId = configuration["Auth:Google:ClientId"] ?? string.Empty;
            options.ClientSecret = configuration["Auth:Google:ClientSecret"] ?? string.Empty;
        });

        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = configuration["Jwt:Issuer"] ?? options.Issuer;
            options.Audience = configuration["Jwt:Audience"] ?? options.Audience;
            options.SigningKey = configuration["Jwt:SigningKey"] ?? options.SigningKey;

            if (int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var accessTokenMinutes))
            {
                options.AccessTokenMinutes = accessTokenMinutes;
            }
        });

        services.Configure<StorageOptions>(options =>
        {
            options.BucketName = configuration["AWS:S3:BucketName"] ?? string.Empty;

            if (int.TryParse(configuration["AWS:S3:UploadUrlExpirationMinutes"], out var uploadExpiry))
            {
                options.UploadUrlExpirationMinutes = uploadExpiry;
            }

            if (int.TryParse(configuration["AWS:S3:ViewUrlExpirationMinutes"], out var viewExpiry))
            {
                options.ViewUrlExpirationMinutes = viewExpiry;
            }
        });

        var awsRegion = configuration["AWS:Region"];
        var awsAccessKeyId = configuration["AWS:AccessKeyId"];
        var awsSecretAccessKey = configuration["AWS:SecretAccessKey"];

        services.AddSingleton<IAmazonS3>(_ =>
        {
            if (string.IsNullOrWhiteSpace(awsRegion))
            {
                throw new InvalidOperationException("AWS:Region must be configured.");
            }

            var regionEndpoint = RegionEndpoint.GetBySystemName(awsRegion);

            if (!string.IsNullOrWhiteSpace(awsAccessKeyId) && !string.IsNullOrWhiteSpace(awsSecretAccessKey))
            {
                var credentials = new BasicAWSCredentials(awsAccessKeyId, awsSecretAccessKey);
                return new AmazonS3Client(credentials, regionEndpoint);
            }

            return new AmazonS3Client(regionEndpoint);
        });

        services.AddScoped<IGoogleTokenVerifier, GoogleTokenVerifier>();
        services.AddScoped<IUserIdentityStore, EfUserIdentityStore>();
        services.AddScoped<IPrivateMediaUrlService, S3PresignedUrlService>();
        services.AddScoped<IWardrobeItemRepository, EfWardrobeItemRepository>();
        services.AddScoped<IWishlistItemRepository, EfWishlistItemRepository>();
        services.AddScoped<IMediaAssetRepository, EfMediaAssetRepository>();
        services.AddScoped<AuthSessionService>();
        services.AddScoped<CreateWardrobeItemCommand>();
        services.AddScoped<CreateWishlistItemCommand>();

        return services;
    }
}