using Shouldly;
using StateAnimationPlugin.Managers;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Characterization tests pinning <see cref="BitmapLoader"/> after it was drained from a
/// <c>Singleton&lt;T&gt;</c> to an injectable <see cref="IBitmapLoader"/>. WPF PNG decode runs
/// fine on the default (MTA) test thread, so no STA wrapper is needed here.
/// </summary>
public class BitmapLoaderTests : BaseTestClass
{
    [Fact]
    public void LoadImage_caches_and_returns_the_same_frame_for_a_repeated_resource()
    {
        BitmapLoader loader = new BitmapLoader();

        var first = loader.LoadImage("PlayIcon.png");
        var second = loader.LoadImage("PlayIcon.png");

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void LoadImage_decodes_an_embedded_resource_into_a_frame()
    {
        BitmapLoader loader = new BitmapLoader();

        var frame = loader.LoadImage("PlayIcon.png");

        frame.ShouldNotBeNull();
        frame.PixelWidth.ShouldBeGreaterThan(0);
        frame.PixelHeight.ShouldBeGreaterThan(0);
    }
}
