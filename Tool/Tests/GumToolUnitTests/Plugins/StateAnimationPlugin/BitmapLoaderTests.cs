using Shouldly;
using StateAnimationPlugin.Managers;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Characterization tests pinning <see cref="BitmapLoader"/> after it was drained from a
/// <c>Singleton&lt;T&gt;</c> to an injectable <see cref="IBitmapLoader"/>.
/// </summary>
public class BitmapLoaderTests : BaseTestClass
{
    [Fact]
    public void LoadImage_caches_and_returns_the_same_bytes_for_a_repeated_resource()
    {
        BitmapLoader loader = new BitmapLoader();

        byte[] first = loader.LoadImage("PlayIcon.png");
        byte[] second = loader.LoadImage("PlayIcon.png");

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void LoadImage_reads_an_embedded_resource_into_non_empty_bytes()
    {
        BitmapLoader loader = new BitmapLoader();

        byte[] bytes = loader.LoadImage("PlayIcon.png");

        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
    }
}
