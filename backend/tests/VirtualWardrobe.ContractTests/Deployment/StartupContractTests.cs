using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using VirtualWardrobe.Api.Controllers;
using VirtualWardrobe.Api.Infrastructure;

namespace VirtualWardrobe.ContractTests.Deployment;

public sealed class StartupContractTests
{
    [Fact]
    public void ShouldRunMigrationsOnStartupDefaultsToFalseWhenUnset()
    {
        var configuration = BuildConfiguration(value: null);

        Assert.False(DatabaseMigrationExtensions.ShouldRunMigrationsOnStartup(configuration));
    }

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    public void ShouldRunMigrationsOnStartupIsFalseWhenDisabled(string value)
    {
        var configuration = BuildConfiguration(value);

        Assert.False(DatabaseMigrationExtensions.ShouldRunMigrationsOnStartup(configuration));
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    public void ShouldRunMigrationsOnStartupIsTrueWhenEnabled(string value)
    {
        var configuration = BuildConfiguration(value);

        Assert.True(DatabaseMigrationExtensions.ShouldRunMigrationsOnStartup(configuration));
    }

    [Fact]
    public void HealthEndpointReturnsOkStatus()
    {
        var controller = new HealthController();

        var action = controller.Get();

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        var payload = Assert.IsType<HealthResponse>(ok.Value);
        Assert.Equal("ok", payload.Status);
    }

    private static IConfiguration BuildConfiguration(string? value)
    {
        var data = new Dictionary<string, string?>();
        if (value is not null)
        {
            data[DatabaseMigrationExtensions.RunMigrationsOnStartupKey] = value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }
}
