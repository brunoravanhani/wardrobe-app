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

        Assert.DoesNotContain(constructorParameterTypes, type => type == typeof(VirtualWardrobeDbContext));
        Assert.All(constructorParameterTypes, type => Assert.True(type.IsInterface));
    }
}
