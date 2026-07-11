using SkiaGum.Content;
using SkiaSharp;
using Shouldly;
using System;
using System.IO;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit;

namespace SkiaGum.Tests.Content;

public class SkiaResourceManagerTests
{
    // Regression for #3609: the SKBitmap cache used to be a plain, non-concurrent
    // Dictionary. xUnit runs test classes in parallel, so concurrent GetSKBitmap
    // calls raced on the Dictionary's internal state during a resize, and a
    // just-inserted key could read back as missing → KeyNotFoundException. Hammer
    // the shared static cache from many threads with distinct keys (which forces
    // adds + resizes) to expose the race; a thread-safe cache loads every bitmap
    // without throwing.
    [Fact]
    public void GetSKBitmap_ConcurrentAccessWithDistinctKeys_ShouldNotCorruptCache()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumSkiaResMgrConcTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        string savedRelativeDirectory = FileManager.RelativeDirectory;

        try
        {
            const int fileCount = 500;
            string[] absolutePaths = new string[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                string absolutePath = Path.Combine(tempRoot, "conc_" + i + ".png");
                using (SKBitmap source = new SKBitmap(2, 2))
                using (SKImage image = SKImage.FromBitmap(source))
                using (SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100))
                using (FileStream fileStream = File.OpenWrite(absolutePath))
                {
                    encoded.SaveTo(fileStream);
                }
                absolutePaths[i] = absolutePath;
            }

            FileManager.RelativeDirectory = tempRoot + Path.DirectorySeparatorChar;

            ParallelOptions options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(4, Environment.ProcessorCount * 2)
            };

            Parallel.For(0, fileCount, options, i =>
            {
                SKBitmap loaded = SkiaResourceManager.GetSKBitmap(absolutePaths[i]);
                loaded.ShouldNotBeNull();
            });
        }
        finally
        {
            FileManager.RelativeDirectory = savedRelativeDirectory;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    // Regression: CacheSKImage used to do `RelativeDirectory + resourceName`
    // unconditionally, so any caller passing an already-absolute path (e.g.
    // AnimationFrame.ToAnimationFrame, which pre-prefixes RelativeDirectory
    // before invoking LoadContent) produced a doubled path that File.Exists
    // couldn't resolve. The loader then fell through to the embedded-resource
    // branch and threw on the mangled name. Fix guards the prepend behind
    // FileManager.IsRelative(...).
    [Fact]
    public void GetSKBitmap_WithAbsolutePath_ShouldLoadFromDisk()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumSkiaResMgrTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        string savedRelativeDirectory = FileManager.RelativeDirectory;

        try
        {
            string fileName = "skia_loader_test.png";
            string absolutePath = Path.Combine(tempRoot, fileName);

            // Encode a tiny SKBitmap to disk so the loader has a real PNG to read.
            using (SKBitmap source = new SKBitmap(3, 3))
            using (SKImage image = SKImage.FromBitmap(source))
            using (SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100))
            using (FileStream fileStream = File.OpenWrite(absolutePath))
            {
                encoded.SaveTo(fileStream);
            }

            // Simulate what AnimationChainList.ToAnimationChainList does — set
            // RelativeDirectory to the .achx's folder. The loader must NOT
            // re-prepend this onto an already-absolute resource name.
            FileManager.RelativeDirectory = tempRoot + Path.DirectorySeparatorChar;

            SKBitmap loaded = SkiaResourceManager.GetSKBitmap(absolutePath);

            loaded.ShouldNotBeNull();
            loaded.Width.ShouldBe(3);
            loaded.Height.ShouldBe(3);
        }
        finally
        {
            FileManager.RelativeDirectory = savedRelativeDirectory;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
