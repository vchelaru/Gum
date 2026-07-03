using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Content;

public class ContentLoaderTests : BaseTestClass
{
    // #3496: a font's atlas texture must pick up the project's DefaultTextureFilter instead of a
    // hardcoded point filter, mirroring how sprite textures are already handled (LoadTextureFromFile).
    // Exercises the on-disk .fnt branch, which loads via raylib's native Raylib.LoadFont rather than
    // BuildFont. Raylib exposes no getter for a texture's applied filter (SetTextureFilter is a
    // write-only GPU call), so ContentLoader.TextureFilterApplier is a test seam that records what
    // was applied instead of actually issuing the GPU call.
    [Fact]
    public void LoadContent_Font_FromDiskFnt_ShouldApplyDefaultTextureFilterToTexture()
    {
        Raylib_cs.TextureFilter savedDefaultFilter = ContentLoader.DefaultTextureFilter;
        Action<Texture2D, Raylib_cs.TextureFilter> savedApplier = ContentLoader.TextureFilterApplier;
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        try
        {
            ContentLoader.DefaultTextureFilter = Raylib_cs.TextureFilter.Bilinear;
            LoaderManager.Self.CacheTextures = false;

            List<(uint TextureId, Raylib_cs.TextureFilter Filter)> appliedFilters = new();
            ContentLoader.TextureFilterApplier = (texture, filter) => appliedFilters.Add((texture.Id, filter));

            string fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "FontCache");
            string fntPath = Path.Combine(fixtureDirectory, "Font18Arial.fnt");

            Font font = LoaderManager.Self.ContentLoader.LoadContent<Font>(fntPath);

            appliedFilters.ShouldContain((font.Texture.Id, Raylib_cs.TextureFilter.Bilinear));
        }
        finally
        {
            ContentLoader.DefaultTextureFilter = savedDefaultFilter;
            ContentLoader.TextureFilterApplier = savedApplier;
            LoaderManager.Self.CacheTextures = savedCacheTextures;
        }
    }

    // Companion to the disk-loaded case above: the stream-hook path funnels through BuildFont
    // (shared with KernSmith's in-memory font creation), which is where the filter is applied for
    // fonts that never touch raylib's native loader. #3496
    [Fact]
    public void LoadContent_Font_FromStreamHookFnt_ShouldApplyDefaultTextureFilterToTexture()
    {
        string fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "FontCache");
        byte[] fntBytes = File.ReadAllBytes(Path.Combine(fixtureDirectory, "Font18Arial.fnt"));
        byte[] pageBytes = File.ReadAllBytes(Path.Combine(fixtureDirectory, "Font18Arial_0.png"));

        Raylib_cs.TextureFilter savedDefaultFilter = ContentLoader.DefaultTextureFilter;
        Action<Texture2D, Raylib_cs.TextureFilter> savedApplier = ContentLoader.TextureFilterApplier;
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        Func<string, Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        try
        {
            ContentLoader.DefaultTextureFilter = Raylib_cs.TextureFilter.Bilinear;
            LoaderManager.Self.CacheTextures = false;

            List<(uint TextureId, Raylib_cs.TextureFilter Filter)> appliedFilters = new();
            ContentLoader.TextureFilterApplier = (texture, filter) => appliedFilters.Add((texture.Id, filter));

            FileManager.CustomGetStreamFromFile = incomingPath =>
            {
                string fileNameOnly = Path.GetFileName(incomingPath);
                if (string.Equals(fileNameOnly, "Font18Arial.fnt", StringComparison.OrdinalIgnoreCase))
                {
                    return new MemoryStream(fntBytes);
                }
                if (string.Equals(fileNameOnly, "Font18Arial_0.png", StringComparison.OrdinalIgnoreCase))
                {
                    return new MemoryStream(pageBytes);
                }
                // null is the hook's documented "I don't have this file" signal.
                return null!;
            };

            string notOnDiskFntPath = Path.Combine(Path.GetTempPath(),
                "GumRaylibFontFilterHookTest_" + Guid.NewGuid().ToString("N"), "Font18Arial.fnt");

            Font font = LoaderManager.Self.ContentLoader.LoadContent<Font>(notOnDiskFntPath);

            appliedFilters.ShouldContain((font.Texture.Id, Raylib_cs.TextureFilter.Bilinear));
        }
        finally
        {
            ContentLoader.DefaultTextureFilter = savedDefaultFilter;
            ContentLoader.TextureFilterApplier = savedApplier;
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.CustomGetStreamFromFile = savedHook;
        }
    }

    // The stream-hook path must not let GetStreamForFile's FileNotFoundException escape when the
    // .fnt is on neither disk nor the hook: the load falls back to an empty Font. The hook here is
    // present but serves nothing, exercising the catch/fallback rather than the no-hook path. #3037
    [Fact]
    public void LoadContent_Font_WhenFntMissingFromDiskAndHook_ShouldReturnEmptyFontWithoutThrowing()
    {
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        Func<string, Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        try
        {
            LoaderManager.Self.CacheTextures = false;
            FileManager.CustomGetStreamFromFile = _ => null!;

            string notOnDiskFntPath = Path.Combine(Path.GetTempPath(),
                "GumRaylibFontMissingTest_" + Guid.NewGuid().ToString("N"), "Missing.fnt");

            Font font = default;
            Should.NotThrow(() =>
                font = LoaderManager.Self.ContentLoader.LoadContent<Font>(notOnDiskFntPath));

            font.GlyphCount.ShouldBe(0);
        }
        finally
        {
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.CustomGetStreamFromFile = savedHook;
        }
    }

    // The stream-hook path is only taken when the .fnt is absent from disk; an on-disk .fnt still
    // loads via raylib's native LoadFont, so a normal game's font loading is unchanged. #3037
    [Fact]
    public void LoadContent_Font_WhenFntOnDisk_ShouldStillLoadViaNativePath()
    {
        string fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "FontCache");
        string fntPath = Path.Combine(fixtureDirectory, "Font18Arial.fnt");

        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        Func<string, Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        try
        {
            LoaderManager.Self.CacheTextures = false;
            FileManager.CustomGetStreamFromFile = null;

            Font font = LoaderManager.Self.ContentLoader.LoadContent<Font>(fntPath);

            font.GlyphCount.ShouldBeGreaterThan(0);
        }
        finally
        {
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.CustomGetStreamFromFile = savedHook;
        }
    }

    // The line-metrics registry (which lets a raylib Text recover the .fnt's lineHeight/base, since
    // Raylib_cs.Font has no field for them) is keyed by atlas texture id. Loading a .fnt registers an
    // entry; that entry MUST be dropped when the font is unloaded, because raylib can later hand the
    // freed texture id to a different font — a stale entry would then give that font the wrong line
    // height. This pins the load-registers / dispose-removes lifecycle.
    [Fact]
    public void ManagedFont_Dispose_ShouldRemoveRegisteredLineMetrics()
    {
        string fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "FontCache");
        string fntPath = Path.Combine(fixtureDirectory, "Font18Arial.fnt");

        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        Func<string, Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        try
        {
            // No caching: we own the only ManagedFont for this load, so disposing it is the sole
            // UnloadFont (no cached copy to double-free) and the registry entry's lifetime is ours.
            LoaderManager.Self.CacheTextures = false;
            FileManager.CustomGetStreamFromFile = null;

            Font font = LoaderManager.Self.ContentLoader.LoadContent<Font>(fntPath);
            uint textureId = font.Texture.Id;

            // Loaded: Font18Arial.fnt's lineHeight/base are registered against the atlas texture id.
            RaylibFontMetricsRegistry.TryGet(textureId, out _).ShouldBeTrue();

            ManagedFont managedFont = new ManagedFont(font);
            managedFont.Dispose();

            // Unloaded: the entry is gone, so a reused texture id cannot return stale metrics.
            RaylibFontMetricsRegistry.TryGet(textureId, out _).ShouldBeFalse();
        }
        finally
        {
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.CustomGetStreamFromFile = savedHook;
        }
    }

    // Regression for #3037: a bundled bitmap font (.fnt + .png page) loaded via LoadFont used to
    // bypass CustomGetStreamFromFile entirely — raylib's path-based LoadFont reads straight off
    // disk. Here both files exist ONLY in memory (served through the hook) and the path handed to
    // LoadContent points at a directory that does not exist on disk, so a populated Font proves
    // both the .fnt text AND its page were pulled through the hook.
    [Fact]
    public void LoadContent_Font_WithBundledFntServedThroughHook_ShouldLoadGlyphsAndPage()
    {
        string fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "FontCache");
        byte[] fntBytes = File.ReadAllBytes(Path.Combine(fixtureDirectory, "Font18Arial.fnt"));
        byte[] pageBytes = File.ReadAllBytes(Path.Combine(fixtureDirectory, "Font18Arial_0.png"));

        Dictionary<string, byte[]> inMemoryFiles = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "Font18Arial.fnt", fntBytes },
            { "Font18Arial_0.png", pageBytes },
        };

        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        Func<string, Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        try
        {
            // Disable caching so a prior load of the same logical name can't mask the result.
            LoaderManager.Self.CacheTextures = false;
            FileManager.CustomGetStreamFromFile = incomingPath =>
            {
                string fileNameOnly = Path.GetFileName(incomingPath);
                if (inMemoryFiles.TryGetValue(fileNameOnly, out byte[]? bytes))
                {
                    return new MemoryStream(bytes);
                }
                // null is the hook's documented "I don't have this file" signal.
                return null!;
            };

            // A directory that does not exist on disk forces resolution through the hook.
            string notOnDiskFntPath = Path.Combine(Path.GetTempPath(),
                "GumRaylibFontHookTest_" + Guid.NewGuid().ToString("N"), "Font18Arial.fnt");

            Font font = LoaderManager.Self.ContentLoader.LoadContent<Font>(notOnDiskFntPath);

            font.GlyphCount.ShouldBe(191);
            font.Texture.Width.ShouldBe(256);
            font.Texture.Height.ShouldBe(256);

            // Spot-check that one glyph's metrics were mapped field-for-field (not just counted).
            // '!' (id=33) in Font18Arial.fnt: x=0 y=92 width=4 height=13 xoffset=1 yoffset=4
            // xadvance=6 — deliberately a glyph whose width != height and xoffset != yoffset, so a
            // transposed field is caught. Queried through raylib's own lookup, which also proves
            // the glyph's Value was set so the glyph is findable.
            Rectangle atlasRec = Raylib.GetGlyphAtlasRec(font, '!');
            atlasRec.X.ShouldBe(0f);
            atlasRec.Y.ShouldBe(92f);
            atlasRec.Width.ShouldBe(4f);
            atlasRec.Height.ShouldBe(13f);

            GlyphInfo glyph = Raylib.GetGlyphInfo(font, '!');
            glyph.Value.ShouldBe(33);
            glyph.OffsetX.ShouldBe(1);
            glyph.OffsetY.ShouldBe(4);
            glyph.AdvanceX.ShouldBe(6);
        }
        finally
        {
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.CustomGetStreamFromFile = savedHook;
        }
    }

    // #3496: a multi-page .fnt loaded through the stream hook must fail loudly instead of silently
    // building a Font against only page 0 (glyphs on page 1+ would get real .fnt coordinates mapped
    // onto the wrong atlas texture, mis-rendering with no error). Merging pages here isn't supported —
    // that only happens natively when raylib loads the .fnt straight off disk.
    [Fact]
    public void LoadContent_Font_WithMultiPageFntServedThroughHook_ShouldThrowNotSupportedException()
    {
        const string multiPageFntText =
            "info face=\"Arial\" size=-18 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
            "common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=2 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n" +
            "page id=0 file=\"Page0.png\"\n" +
            "page id=1 file=\"Page1.png\"\n" +
            "chars count=1\n" +
            "char id=65   x=0     y=0     width=4     height=13    xoffset=1     yoffset=4     xadvance=6     page=0  chnl=15\n";

        Func<string, Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        try
        {
            FileManager.CustomGetStreamFromFile = incomingPath =>
            {
                string fileNameOnly = Path.GetFileName(incomingPath);
                if (string.Equals(fileNameOnly, "MultiPage.fnt", StringComparison.OrdinalIgnoreCase))
                {
                    return new MemoryStream(Encoding.UTF8.GetBytes(multiPageFntText));
                }
                // null is the hook's documented "I don't have this file" signal.
                return null!;
            };

            string notOnDiskFntPath = Path.Combine(Path.GetTempPath(),
                "GumRaylibMultiPageFontTest_" + Guid.NewGuid().ToString("N"), "MultiPage.fnt");

            Should.Throw<NotSupportedException>(() =>
                LoaderManager.Self.ContentLoader.LoadContent<Font>(notOnDiskFntPath));
        }
        finally
        {
            FileManager.CustomGetStreamFromFile = savedHook;
        }
    }

    // Companion to the multi-page case above: a single-page .fnt served the same way must keep
    // loading successfully — the new guard is scoped to pageFileNames.Length > 1. #3496
    [Fact]
    public void LoadContent_Font_WithSinglePageFntServedThroughHook_ShouldNotThrow()
    {
        string fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "FontCache");
        byte[] fntBytes = File.ReadAllBytes(Path.Combine(fixtureDirectory, "Font18Arial.fnt"));
        byte[] pageBytes = File.ReadAllBytes(Path.Combine(fixtureDirectory, "Font18Arial_0.png"));

        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        Func<string, Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        try
        {
            LoaderManager.Self.CacheTextures = false;
            FileManager.CustomGetStreamFromFile = incomingPath =>
            {
                string fileNameOnly = Path.GetFileName(incomingPath);
                if (string.Equals(fileNameOnly, "Font18Arial.fnt", StringComparison.OrdinalIgnoreCase))
                {
                    return new MemoryStream(fntBytes);
                }
                if (string.Equals(fileNameOnly, "Font18Arial_0.png", StringComparison.OrdinalIgnoreCase))
                {
                    return new MemoryStream(pageBytes);
                }
                // null is the hook's documented "I don't have this file" signal.
                return null!;
            };

            string notOnDiskFntPath = Path.Combine(Path.GetTempPath(),
                "GumRaylibSinglePageFontTest_" + Guid.NewGuid().ToString("N"), "Font18Arial.fnt");

            Font font = default;
            Should.NotThrow(() =>
                font = LoaderManager.Self.ContentLoader.LoadContent<Font>(notOnDiskFntPath));

            font.GlyphCount.ShouldBe(191);
        }
        finally
        {
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.CustomGetStreamFromFile = savedHook;
        }
    }

    // Pins the missing-file semantics that changed with #3033. The old path-based LoadImage
    // returned a silent empty Image for a missing file; routing through FileManager.GetStreamForFile
    // means a missing file now throws (an IOException), matching the MonoGame-family loader. Callers
    // such as Sprite source assignment wrap LoadContent in a catch and honor
    // GraphicalUiElement.MissingFileBehavior, so this throw is the intended contract — not a leak.
    [Fact]
    public void LoadContent_WhenFileMissingAndNoHook_ShouldThrow()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumRaylibMissingFileTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        string savedRelativeDirectory = FileManager.RelativeDirectory;
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;

        try
        {
            // Empty temp folder + no hook → the resolved path exists nowhere, so the load must throw.
            FileManager.RelativeDirectory = tempRoot.Replace('\\', '/') + "/";
            LoaderManager.Self.CacheTextures = false;
            FileManager.CustomGetStreamFromFile = null;

            Should.Throw<Exception>(() =>
                LoaderManager.Self.ContentLoader.LoadContent<Texture2D>("does_not_exist.png"));
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

    // TryLoadContent was changed alongside #3033 to route through the same hook-honoring path as
    // LoadContent. Its "Try" contract is to swallow load failures and return default rather than
    // propagate the IOException GetStreamForFile throws for a missing file. Pins that no-throw path.
    [Fact]
    public void TryLoadContent_WhenFileMissing_ShouldReturnDefaultWithoutThrowing()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumRaylibTryLoadMissingTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        string savedRelativeDirectory = FileManager.RelativeDirectory;
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;

        try
        {
            FileManager.RelativeDirectory = tempRoot.Replace('\\', '/') + "/";
            LoaderManager.Self.CacheTextures = false;
            FileManager.CustomGetStreamFromFile = null;

            Texture2D loaded = default;
            Should.NotThrow(() =>
                loaded = LoaderManager.Self.ContentLoader.TryLoadContent<Texture2D>("does_not_exist.png"));

            // default(Texture2D) is a zeroed struct — no width/height/pixels.
            loaded.Width.ShouldBe(0);
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

    // The companion to the missing-file case: when bytes are available only through the hook,
    // TryLoadContent must honor it (the old implementation called raylib's path-based LoadImage and
    // ignored the hook entirely).
    [Fact]
    public void TryLoadContent_WhenTextureAvailableViaCustomGetStreamFromFile_ShouldLoadFromHook()
    {
        byte[] pngBytes = CreatePngBytes(5, 6);

        string tempRoot = Path.Combine(Path.GetTempPath(), "GumRaylibTryLoadHookTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        string savedRelativeDirectory = FileManager.RelativeDirectory;
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;

        try
        {
            string fileName = "try_hooked_pixel.png";
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

            Texture2D loaded = LoaderManager.Self.ContentLoader.TryLoadContent<Texture2D>(fileName);

            hookWasInvoked.ShouldBeTrue();
            loaded.Width.ShouldBe(5);
            loaded.Height.ShouldBe(6);
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
}
