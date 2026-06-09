using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace VirtualWardrobe.Api.Observability;

public static class TelemetryConfig
{
    public const string ServiceName = "VirtualWardrobe.Api";

    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    // Auth
    public static readonly Counter<long> AuthExchangeTotal =
        Meter.CreateCounter<long>("auth.exchange.total", description: "Total Google token exchange attempts");
    public static readonly Counter<long> AuthExchangeFailures =
        Meter.CreateCounter<long>("auth.exchange.failures", description: "Failed Google token exchange attempts");

    // S3 presign
    public static readonly Counter<long> MediaUploadUrlTotal =
        Meter.CreateCounter<long>("media.upload_url.total", description: "Total presigned upload URL requests");
    public static readonly Counter<long> MediaViewUrlTotal =
        Meter.CreateCounter<long>("media.view_url.total", description: "Total presigned view URL requests");
    public static readonly Counter<long> MediaPresignFailures =
        Meter.CreateCounter<long>("media.presign.failures", description: "Failed presigned URL generations");

    // Wishlist conversion
    public static readonly Counter<long> WishlistConversionTotal =
        Meter.CreateCounter<long>("wishlist.conversion.total", description: "Total wishlist-to-wardrobe conversion attempts");
    public static readonly Counter<long> WishlistConversionSuccesses =
        Meter.CreateCounter<long>("wishlist.conversion.successes", description: "Successful wishlist-to-wardrobe conversions");
    public static readonly Counter<long> WishlistConversionFailures =
        Meter.CreateCounter<long>("wishlist.conversion.failures", description: "Failed wishlist-to-wardrobe conversions");

    public static IServiceCollection AddTelemetry(this IServiceCollection services)
    {
        services.AddSingleton(ActivitySource);
        services.AddSingleton(Meter);
        return services;
    }
}
