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
