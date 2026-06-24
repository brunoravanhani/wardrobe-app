using Microsoft.EntityFrameworkCore;
using VirtualWardrobe.Infrastructure.Persistence;

namespace VirtualWardrobe.Api.Infrastructure;

public static partial class DatabaseMigrationExtensions
{
    public const string RunMigrationsOnStartupKey = "RunMigrationsOnStartup";

    /// <summary>
    /// Returns whether pending EF Core migrations should be applied during startup,
    /// driven by the <c>RunMigrationsOnStartup</c> configuration flag. Defaults to false
    /// so local and test environments are unaffected unless explicitly opted in.
    /// </summary>
    public static bool ShouldRunMigrationsOnStartup(IConfiguration configuration)
    {
        return configuration.GetValue<bool>(RunMigrationsOnStartupKey);
    }

    /// <summary>
    /// Applies pending EF Core migrations before the app serves traffic, but only when
    /// <c>RunMigrationsOnStartup</c> is enabled. No-op otherwise.
    /// </summary>
    public static WebApplication MigrateDatabaseIfEnabled(this WebApplication app)
    {
        if (!ShouldRunMigrationsOnStartup(app.Configuration))
        {
            return app;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VirtualWardrobeDbContext>();

        Log.MigrationsStarting(app.Logger);
        dbContext.Database.Migrate();
        Log.MigrationsApplied(app.Logger);

        return app;
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Applying database migrations on startup.")]
        internal static partial void MigrationsStarting(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information, Message = "Database migrations applied.")]
        internal static partial void MigrationsApplied(ILogger logger);
    }
}
