using Raylib_cs;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests;

/// <summary>
/// Covers <see cref="SystemManagers.GetOrLoadEmbeddedTexture2d"/>'s cache-hit branch. Raylib is the
/// backend where this matters most: its <c>LoadEmbeddedTexture2d</c> uploads a fresh GPU texture on
/// every call and caches nothing, so repeated <c>Styling</c> construction leaked one texture per
/// instance. Issue #4451.
/// </summary>
public class GetOrLoadEmbeddedTexture2dTests : BaseTestClass
{
    [Fact]
    public void GetOrLoadEmbeddedTexture2d_CalledTwice_ShouldReturnTheSameTexture()
    {
        SystemManagers systemManagers = SystemManagers.Default;

        Texture2D first = systemManagers.GetOrLoadEmbeddedTexture2d("UISpriteSheet.png");
        Texture2D second = systemManagers.GetOrLoadEmbeddedTexture2d("UISpriteSheet.png");

        first.Id.ShouldBeGreaterThan(0u);
        second.Id.ShouldBe(first.Id);
    }
}
