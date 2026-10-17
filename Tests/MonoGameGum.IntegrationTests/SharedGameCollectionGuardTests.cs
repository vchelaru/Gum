using System.Reflection;
using Microsoft.Xna.Framework;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests;

/// <summary>
/// A real <see cref="Game"/> created and disposed while <see cref="SharedGame"/> is alive leaves the
/// shared device without a current GL context, so every later draw on it silently renders nothing
/// (#6092). A class in the shared collection must take a <see cref="SharedGameLease"/> instead.
/// </summary>
public class SharedGameCollectionGuardTests
{
    [Fact]
    public void ClassesInTheSharedCollection_ShouldNotDeclareTheirOwnGame()
    {
        List<string> offenders = new List<string>();
        foreach (Type type in typeof(SharedGame).Assembly.GetTypes())
        {
            if (type.IsNested)
            {
                continue;
            }
            CollectionAttribute? collection = type.GetCustomAttribute<CollectionAttribute>();
            if (collection?.Name != SharedGame.CollectionName)
            {
                continue;
            }
            foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (typeof(Game).IsAssignableFrom(nested))
                {
                    offenders.Add($"{type.Name}.{nested.Name}");
                }
            }
        }

        offenders.ShouldBeEmpty(
            "these shared-collection test classes declare their own Game; derive from SharedGameLease instead");
    }
}
