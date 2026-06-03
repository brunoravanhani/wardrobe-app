using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtualWardrobe.Infrastructure.Persistence.Entities;

namespace VirtualWardrobe.Infrastructure.Persistence;

public sealed class VirtualWardrobeDbContext : DbContext
{
    public VirtualWardrobeDbContext(DbContextOptions<VirtualWardrobeDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserRecord> Users => Set<UserRecord>();

    public DbSet<MediaAssetRecord> MediaAssets => Set<MediaAssetRecord>();

    public DbSet<WardrobeItemRecord> WardrobeItems => Set<WardrobeItemRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VirtualWardrobeDbContext).Assembly);
    }
}

public static class VirtualWardrobeDbContextRegistration
{
    public static IServiceCollection AddVirtualWardrobePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Default must be configured.");
        }

        services.AddDbContext<VirtualWardrobeDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}