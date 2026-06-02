using Raylib_cs;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Content;

public class ContentLoaderTests : BaseTestClass
{
    // Regression (#3033): the Raylib loader handed the path straight to raylib's LoadImage, which
    // opens the file off disk itself, so FileManager.CustomGetStreamFromFile was a no-op on Raylib.
    // Any feature built on that hook (.gumpkg bundles, the GumFromZipFile sample, mobile
    // TitleContainer redirection, encrypted/in-memory asset stores) silently did nothing. The fix
    // routes texture loads through FileManager.GetStreamForFile (which consults the hook) and feeds
    // the bytes to raylib's in-memory loader. Here the texture exists ONLY via the hook — there is
    // no file on disk at the resolved path — so a successful load proves the hook was honored.
    [Fact]
    public void LoadContent_WhenTextureOnlyAvailableViaCustomGetStreamFromFile_ShouldLoadFromHook()
    {
        byte[] pngBytes = CreatePngBytes(3, 4);

        string tempRoot = Path.Combine(Path.GetTempPath(), "GumRaylibStreamHookTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        string savedRelativeDirectory = FileManager.RelativeDirectory;
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;

        try
        {
            string fileName = "hooked_only_pixel.png";

            // Point RelativeDirectory at an empty temp folder so the resolved absolute path does
            // NOT exist on disk — the hook is the only way to satisfy the load.
            FileManager.RelativeDirectory = tempRoot.Replace('\\', '/') + "/";
            LoaderManager.Self.CacheTextures = false;

            bool hookWasInvoked = false;
            FileManager.CustomGetStreamFromFile = requestedPath =>
            {
                if (requestedPath.Replace('\\', '/').EndsWith(fileName))
                {
                    hookWasInvoked = true;
                    return new MemoryStream(pngBytes);
                }
                // null is the hook's documented "I don't have this file" signal.
                return null!;
            };

            Texture2D loaded = LoaderManager.Self.ContentLoader.LoadContent<Texture2D>(fileName);

            hookWasInvoked.ShouldBeTrue();
            loaded.Width.ShouldBe(3);
            loaded.Height.ShouldBe(4);
        }
        finally
        {
            FileManager.CustomGetStreamFromFile = null;
            FileManager.RelativeDirectory = savedRelativeDirectory;
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    // Encodes a real PNG into memory by exporting a generated image to a throwaway temp file and
    // reading the bytes back — raylib has no public encode-to-byte[] helper exposed here, and this
    // matches the GenImageColor/ExportImage idiom already used below.
    private static byte[] CreatePngBytes(int width, int height)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), "GumRaylibStreamHookSource_" + Guid.NewGuid().ToString("N") + ".png");
        Image image = Raylib.GenImageColor(width, height, Raylib_cs.Color.Blue);
        try
        {
            Raylib.ExportImage(image, tempFile);
            return File.ReadAllBytes(tempFile);
        }
        finally
        {
            Raylib.UnloadImage(image);
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    // Regression: ContentLoader.LoadTexture2D used to cache by the relative-directory-prefixed
    // name but actually call LoadImage with the bare filename, so any caller that relied on
    // FileManager.RelativeDirectory (notably AnimationChainList.ToAnimationChainList, which sets
    // RelativeDirectory to the .achx's folder before loading per-frame textures) silently got a
    // zero-size texture and Sprite.Render early-returned on null Texture. The fix loads using
    // the standardized (prefixed) path, matching what the cache stores.
    [Fact]
    public void LoadContent_WithRelativeDirectorySet_ShouldResolveTextureRelativeToThatDirectory()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumRaylibContentLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        string savedRelativeDirectory = FileManager.RelativeDirectory;
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;

        try
        {
            string fileName = "loader_test_pixel.png";
            string fullPath = Path.Combine(tempRoot, fileName);

            // GenImageColor + ExportImage is the cheapest way to drop a real PNG on disk
            // without pulling in a test-fixture asset.
            Image image = Raylib.GenImageColor(2, 2, Raylib_cs.Color.Red);
            try
            {
                Raylib.ExportImage(image, fullPath);
            }
            finally
            {
                Raylib.UnloadImage(image);
            }

            // FileManager.RelativeDirectory uses forward slashes with a trailing /, matching
            // what AnimationChainList.ToAnimationChainList writes via FileManager.GetDirectory.
            FileManager.RelativeDirectory = tempRoot.Replace('\\', '/') + "/";
            // Disable caching so this test isn't masked by a prior successful load of the
            // same logical name from a different test.
            LoaderManager.Self.CacheTextures = false;

            Texture2D loaded = LoaderManager.Self.ContentLoader.LoadContent<Texture2D>(fileName);

            loaded.Width.ShouldBe(2);
            loaded.Height.ShouldBe(2);
        }
        finally
        {
            FileManager.RelativeDirectory = savedRelativeDirectory;
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
