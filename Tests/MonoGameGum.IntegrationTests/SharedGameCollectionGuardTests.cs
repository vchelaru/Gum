using System.Reflection;
using Microsoft.Xna.Framework;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests;

/// <summary>
/// A real <see cref="Game"/> created and disposed while <see cref="SharedGame"/> is alive leaves the
/// shared device without a current GL context, so every later draw on it silently renders nothing
/// (#6092). Every test takes a <see cref="SharedGameLease"/> instead, and every test class is in the one
/// shared collection, so the assembly creates a single Game (#6101).
/// </summary>
public class SharedGameCollectionGuardTests
{
    [Fact]
    public void TheAssembly_ShouldDeclareNoGameButTheSharedHost()
    {
        List<string> offenders = new List<string>();
        foreach (Type type in typeof(SharedGame).Assembly.GetTypes())
        {
            if (typeof(Game).IsAssignableFrom(type) && type.Name != "HostGame")
            {
                offenders.Add(type.FullName!);
            }
        }

        offenders.ShouldBeEmpty(
            "these types declare their own Game; derive from SharedGameLease instead");
    }

    [Fact]
    public void TestClasses_ShouldNotJoinACollectionOtherThanTheSharedGame()
    {
        List<string> offenders = new List<string>();
        foreach (Type type in typeof(SharedGame).Assembly.GetTypes())
        {
            CollectionAttribute? collection = type.GetCustomAttribute<CollectionAttribute>();
            if (collection != null && collection.Name != SharedGame.CollectionName)
            {
                offenders.Add($"{type.Name}: {collection.Name}");
            }
        }

        offenders.ShouldBeEmpty(
            "a second collection fixture would create a second Game; use the shared collection");
    }
}
