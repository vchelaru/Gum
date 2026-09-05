using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Content;
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

    // The other half of the contract: LoadEmbeddedTexture2d is the always-fresh loader, and the
    // caller owns what it returns. Nothing caches it, so nothing will ever call UnloadTexture on
    // it either -- which is exactly why Styling and FormsUtilities must use the get-or-load method
    // instead. Pins both halves so a future "helpful" cache added here doesn't silently hand two
    // callers the same texture.
    [Fact]
    public void LoadEmbeddedTexture2d_CalledTwice_ShouldUploadTwoUnownedTextures()
    {
        SystemManagers systemManagers = SystemManagers.Default;
        string cacheKey = $"EmbeddedResource.{SystemManagers.AssemblyPrefix}.UISpriteSheet.png";

        // Drop just this key if a neighbouring test cached it, so the assertion below is about this
        // method's own behavior. Scoped to the one entry rather than toggling CacheTextures, which
        // would dispose the whole shared cache and break tests that run after this one.
        LoaderManager.Self.Dispose(cacheKey);

        Texture2D first = systemManagers.LoadEmbeddedTexture2d("UISpriteSheet.png")!.Value;
        Texture2D second = systemManagers.LoadEmbeddedTexture2d("UISpriteSheet.png")!.Value;

        try
        {
            second.Id.ShouldNotBe(first.Id);
            LoaderManager.Self.CachedDisposables.ContainsKey(cacheKey).ShouldBeFalse();
        }
        finally
        {
            Raylib.UnloadTexture(first);
            Raylib.UnloadTexture(second);
        }
    }
}
