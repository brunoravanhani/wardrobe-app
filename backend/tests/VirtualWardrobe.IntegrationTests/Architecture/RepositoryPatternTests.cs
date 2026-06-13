using System.Reflection;
using VirtualWardrobe.Application.Wardrobe;
using VirtualWardrobe.Infrastructure.Persistence;

namespace VirtualWardrobe.IntegrationTests.Architecture;

public sealed class RepositoryPatternTests
{
    [Fact]
    public void CreateWardrobeItemCommandShouldNotDependOnDbContextDirectly()
    {
        var constructor = typeof(CreateWardrobeItemCommand)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();

        var constructorParameterTypes = constructor.GetParameters().Select(x => x.ParameterType).ToArray();

        // The command must never receive the EF DbContext directly — it must go through repository abstractions.
        Assert.DoesNotContain(constructorParameterTypes, type => type == typeof(VirtualWardrobeDbContext));

        // Repository and media-service parameters must be interfaces; application service parameters may be classes.
        var directInfraTypes = constructorParameterTypes
            .Where(t => t.Assembly == typeof(VirtualWardrobeDbContext).Assembly)
            .ToArray();
        Assert.Empty(directInfraTypes);
    }
}
