using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RaylibGum.Renderables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using static Raylib_cs.Raylib;

namespace RenderingLibrary.Content;

/// <summary>
/// Raylib implementation of <see cref="IContentLoader"/>. Loads textures and fonts via Raylib and
/// caches them through <see cref="LoaderManager"/> (wrapped in disposable wrappers, since Raylib's
/// texture/font types are value types).
/// </summary>
/// <remarks>
/// Sealed on purpose: nothing here is virtual, so deriving from it gains nothing. To customize
/// loading, implement <see cref="IContentLoader"/> and delegate to an instance of this class for
/// the content names you do not handle — see the "Custom content loader" section of the file
/// loading docs.
/// </remarks>
public sealed class ContentLoader : IContentLoader
{
    /// <summary>
    /// The texture filter applied to sprite and font textures as they are loaded. Set from the
    /// project's <see cref="Gum.DataTypes.GumProjectSave.TextureFilter"/> during
    /// <c>GumService.Initialize</c> (issue #3199 for sprites, #3496 for fonts). Unlike the XNA-family
    /// backends, raylib has no global sampler state — filtering is a per-texture property applied at
    /// load time — so the loaded value is read here rather than in the Renderer.
    /// </summary>
    public static Raylib_cs.TextureFilter DefaultTextureFilter { get; set; } = Raylib_cs.TextureFilter.Point;

    /// <summary>
    /// Applies a texture filter to a texture. Defaults to the real <see cref="Raylib.SetTextureFilter"/>
    /// call. raylib exposes no getter for a texture's applied filter (it's a write-only GPU call), so
    /// tests substitute this to record which filter was applied to which texture instead of asserting
    /// on GPU state directly.
    /// </summary>
    internal static Action<Texture2D, Raylib_cs.TextureFilter> TextureFilterApplier { get; set; } = SetTextureFilter;

    /// <inheritdoc/>
    public T? LoadContent<T>(string contentName)
    {
        if (typeof(T) == typeof(Texture2D))
        {
            return (T)(object)LoadTexture2D(contentName);
        }
        else if(typeof(T) == typeof(Font))
        {
            return (T)LoadFont(contentName);
        }
        else
        {
            throw new NotImplementedException($"Error attempting to load {contentName} of type {typeof(T).AssemblyQualifiedName}");
        }
    }

    private object LoadFont(string contentName)
    {
        ///////////////////////////////Early Out////////////////////////////////////
        string contentNameStandardized = StandardizeCaseSensitive(contentName);

        if (LoaderManager.Self.CacheTextures)
        {
            var cached = LoaderManager.Self.GetDisposable(contentNameStandardized) as ManagedFont;
            if(cached != null)
            {
                return cached.Font;
            }
        }
        ///////////////////////////////End Early Out////////////////////////////////

        Font? font = null;

        var isFnt = contentName.ToLower().EndsWith(".fnt");
        if (isFnt)
        {
            // Every .fnt goes through Gum's parser, never Raylib.LoadFont: raylib's native loader
            // can't see the CustomGetStreamFromFile hook, can't be reported on when it fails (it
            // crashes the process on a .fnt it can't parse), and Gum merges multi-page atlases itself
            // (#5316). A .fnt that can't be loaded reports through PropertyAssignmentError and falls
            // through to an empty Font, which callers detect via BaseSize == 0.
            try
            {
                font = TryLoadBitmapFont(contentName, registerShadowSibling: true);
            }
            catch (Exception exception)
            {
                CustomSetPropertyOnRenderable.RaisePropertyAssignmentError(
                    $"Could not load bitmap font '{contentName}': {exception.Message}");
            }
            if (font == null)
            {
                font = default(Font);
            }
        }
        else
        {
            // raylib's native loaders take a raw path, so resolve the macOS .app Contents/Resources/
            // copy up front (#5450).
            string? diskPath = FileManager.ResolveExistingFilePath(contentName);
            if (diskPath != null)
            {
                font = LoadFontEx(diskPath, 24, null, 0);
                TextureFilterApplier(font.Value.Texture, DefaultTextureFilter);
            }
        }

        string? ttfPath = font == null ? FileManager.ResolveExistingFilePath(contentName + ".ttf") : null;
        if (ttfPath != null)
        {
            font = LoadFontEx(ttfPath, 24, null, 0);
            TextureFilterApplier(font.Value.Texture, DefaultTextureFilter);
        }

        if(font == null)
        {
            var systemFontPath = GetSystemFontPath(contentName);
            if (File.Exists(systemFontPath))
            {
                font = LoadFontEx(systemFontPath, 24, null, 0);
                TextureFilterApplier(font.Value.Texture, DefaultTextureFilter);
            }
            else
            {
                font = default(Font);
            }
        }

        // Every branch above assigns a value (default(Font) when nothing loads).
        Font loaded = font ?? default(Font);

        if (LoaderManager.Self.CacheTextures)
        {
            var managedFont = new ManagedFont(loaded);

            LoaderManager.Self.AddDisposable(contentNameStandardized, managedFont);
        }


        return loaded;
    }

    private static Texture2D LoadTexture2D(string fileName)
    {
        ///////////////////////////////Early Out////////////////////////////////////

        string fileNameStandardized = StandardizeCaseSensitive(fileName);
        if (LoaderManager.Self.CacheTextures)
        {
            var cached = LoaderManager.Self.GetDisposable(fileNameStandardized) as ManagedTexture;
            if (cached != null)
            {
                return cached.Texture;
            }
        }
        ///////////////////////////////End Early Out////////////////////////////////



        if (FileManager.IsUrl(fileName))
        {
            throw new NotImplementedException("Loading textures from URLs is not implemented yet.");
        }

        // Load via fileNameStandardized so a relative fileName is resolved against
        // FileManager.RelativeDirectory — the same prefix the cache lookup above used.
        // Previously this was just `fileName`, which meant callers relying on
        // RelativeDirectory (e.g. AnimationChainList.ToAnimationChainList loading per-frame
        // textures relative to the .achx's folder) silently got an empty Texture2D and
        // Sprite.Render early-returned on null Texture.
        Texture2D toReturn = LoadTextureFromFile(fileNameStandardized);

        if (LoaderManager.Self.CacheTextures)
        {
            var managedTexture = new ManagedTexture(toReturn);

            LoaderManager.Self.AddDisposable(fileNameStandardized, managedTexture);
        }

        return toReturn;
    }

    private static Texture2D LoadTextureFromFile(string fileName)
    {
        // Route through FileManager.GetStreamForFile so the FileManager.CustomGetStreamFromFile
        // hook is honored on Raylib the same way it is on the MonoGame-family loader. Handing the
        // path straight to raylib's path-based LoadImage bypassed the hook entirely, so .gumpkg
        // bundles, the GumFromZipFile sample, mobile TitleContainer redirection, and any
        // in-memory/encrypted asset store silently failed on Raylib (#3033). raylib's in-memory
        // LoadImageFromMemory takes the file extension (with the leading dot) to pick its decoder.
        string fileType = "." + FileManager.GetExtension(fileName);

        byte[] fileData;
        using (var stream = FileManager.GetStreamForFile(fileName))
        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            fileData = memoryStream.ToArray();
        }

        Image image = LoadImageFromMemory(fileType, fileData);
        // LoadTextureFromImage uploads to the GPU; the CPU-side Image is no longer needed after.
        var toReturn = LoadTextureFromImage(image);
        // Apply the project's texture filter (issue #3199). raylib defaults new textures to point
        // filtering, so this only changes behavior when the project requested linear/bilinear.
        TextureFilterApplier(toReturn, DefaultTextureFilter);
        UnloadImage(image);
        return toReturn;
    }

    // Loads an AngelCode bitmap font (.fnt plus its .png pages) through Gum's own parser. Returns
    // null when nothing can supply the .fnt, letting the caller fall back to default(Font) (#3037);
    // throws when the .fnt is found but can't be parsed or a page can't be read.
    private static Font? TryLoadBitmapFont(string fntPath, bool registerShadowSibling)
    {
        if (!TryReadFntText(fntPath, out string fntText, out string pageDirectory))
        {
            return null;
        }

        Font font = BuildBitmapFont(fntText, pageDirectory);
        if (registerShadowSibling)
        {
            // #5253: a bundled dropshadow font ships its "-shadow.fnt" sibling in the same bundle.
            RegisterShadowSiblingIfPresent(fntPath, font.Texture.Id);
        }
        return font;
    }

    // Reads a .fnt's text, and reports the directory its page names are relative to. The
    // CustomGetStreamFromFile hook wins over a loose file at the same path, as it does for every
    // other content load, so a loaded bundle overrides stale loose copies (#5299). A loose file is
    // read from its resolved path, which may be the macOS .app Contents/Resources/ copy (#5450), so
    // its pages are looked up beside that copy. FileManager's other fallbacks get a last try.
    private static bool TryReadFntText(string fntPath, out string fntText, out string pageDirectory)
    {
        string? hookedText = TryReadTextFromStreamHook(fntPath);
        if (hookedText != null)
        {
            fntText = hookedText;
            pageDirectory = FileManager.GetDirectory(fntPath);
            return true;
        }

        string? diskPath = FileManager.ResolveExistingFilePath(fntPath);
        if (diskPath != null)
        {
            fntText = File.ReadAllText(diskPath);
            pageDirectory = FileManager.GetDirectory(diskPath);
            return true;
        }

        try
        {
            fntText = FileManager.FromFileText(fntPath);
            pageDirectory = FileManager.GetDirectory(fntPath);
            return true;
        }
        catch
        {
            // No hook, or neither the hook nor disk can supply this .fnt.
            fntText = string.Empty;
            pageDirectory = string.Empty;
            return false;
        }
    }

    // The path is normalized the same way GetStreamForFile normalizes it before calling the hook.
    private static string? TryReadTextFromStreamHook(string path)
    {
        Func<string, Stream>? hook = FileManager.CustomGetStreamFromFile;
        if (hook == null)
        {
            return null;
        }

        string hookPath = FileManager.IsUrl(path)
            ? path
            : FileManager.Standardize(path, preserveCase: true, makeAbsolute: true);
        try
        {
            using Stream? stream = hook(hookPath);
            if (stream == null)
            {
                return null;
            }
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    // Parses fntText and loads its page(s) through the hooked texture path. A multi-page font's pages
    // are merged into one stacked texture, because a raylib Font holds a single atlas texture, and
    // every glyph's atlas Y is shifted by its page's offset. Does not probe for a shadow sibling, so
    // loading the sibling itself through here cannot recurse.
    private static Font BuildBitmapFont(string fntText, string pageDirectory)
    {
        ParsedFontFile parsedFontFile = new ParsedFontFile(fntText);

        string[] pageFileNames = parsedFontFile.GetPagesAsArrayOfStrings;
        if (pageFileNames.Length == 0)
        {
            throw new InvalidOperationException("Font file did not list any pages");
        }

        // Checked before any texture is uploaded so a bad glyph can't leak the atlas.
        foreach (FontFileCharLine charLine in parsedFontFile.Chars)
        {
            if (charLine.Page < 0 || charLine.Page >= pageFileNames.Length)
            {
                throw new InvalidOperationException(
                    $"Character {charLine.Id} is on page {charLine.Page}, but the font lists {pageFileNames.Length} page(s)");
            }
        }

        // The page paths are relative to the .fnt; load them through the already-hooked texture path
        // so they resolve from the same bundle/stream as the .fnt.
        if (pageFileNames.Length == 1)
        {
            Texture2D pageTexture = LoadTextureFromFile(pageDirectory + pageFileNames[0]);
            return BuildFont(parsedFontFile, pageTexture);
        }

        int[] pageWidths = new int[pageFileNames.Length];
        int[] pageHeights = new int[pageFileNames.Length];
        byte[][] pagePixels = new byte[pageFileNames.Length][];
        for (int i = 0; i < pageFileNames.Length; i++)
        {
            pagePixels[i] = LoadPageRgba(pageDirectory + pageFileNames[i], out pageWidths[i], out pageHeights[i]);
        }

        BitmapFontAtlasMerger.Layout layout = BitmapFontAtlasMerger.ComputeLayout(pageWidths, pageHeights);
        byte[] atlasPixels = BitmapFontAtlasMerger.MergeRgba(pagePixels, pageWidths, pageHeights, layout);
        Texture2D atlasTexture = UploadRgbaTexture(atlasPixels, layout.Width, layout.Height);
        return BuildFont(parsedFontFile, atlasTexture, layout.PageYOffsets);
    }

    // Reads an image through FileManager.GetStreamForFile (so the stream hook is honored, as in
    // LoadTextureFromFile) and returns its pixels as RGBA, whatever the file's own format was.
    private static unsafe byte[] LoadPageRgba(string pagePath, out int width, out int height)
    {
        byte[] fileData;
        using (var stream = FileManager.GetStreamForFile(pagePath))
        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            fileData = memoryStream.ToArray();
        }

        Image image = LoadImageFromMemory("." + FileManager.GetExtension(pagePath), fileData);
        try
        {
            if (image.Data == null || image.Width <= 0 || image.Height <= 0)
            {
                throw new InvalidOperationException($"Could not decode font page '{pagePath}'");
            }
            Raylib.ImageFormat(ref image, Raylib_cs.PixelFormat.UncompressedR8G8B8A8);
            width = image.Width;
            height = image.Height;
            byte[] pixels = new byte[width * height * BitmapFontAtlasMerger.BytesPerPixel];
            System.Runtime.InteropServices.Marshal.Copy((IntPtr)image.Data, pixels, 0, pixels.Length);
            return pixels;
        }
        finally
        {
            UnloadImage(image);
        }
    }

    private static unsafe Texture2D UploadRgbaTexture(byte[] pixels, int width, int height)
    {
        fixed (byte* pixelPointer = pixels)
        {
            Image image = new Image
            {
                Data = pixelPointer,
                Width = width,
                Height = height,
                Mipmaps = 1,
                Format = Raylib_cs.PixelFormat.UncompressedR8G8B8A8,
            };
            // LoadTextureFromImage copies the pixels to the GPU, so the pinned pointer only needs to
            // stay valid for this call.
            return LoadTextureFromImage(image);
        }
    }

    // Entry point for in-memory font creators in other assemblies (e.g. KernSmith.RaylibGum):
    // parse the .fnt text and assemble a raylib Font around the supplied atlas texture. Exposed
    // (instead of BuildFont) so callers only pass text. See InternalsVisibleTo in
    // Properties/AssemblyInfo.cs.
    // pageYOffsets, when supplied, shifts each glyph's atlas Y by its source page's offset. This
    // lets a single-texture consumer (KernSmith.RaylibGum) merge KernSmith's multiple atlas pages
    // into one stacked texture and still map every glyph correctly — raylib's Font holds one texture.
    internal static Font BuildFontFromFntText(string fntText, Texture2D pageTexture, int[]? pageYOffsets = null)
    {
        return BuildFont(new ParsedFontFile(fntText), pageTexture, pageYOffsets);
    }

    // Assembles a raylib Font from a parsed .fnt and its already-loaded atlas page. The Recs and
    // Glyphs arrays are handed to raylib, which frees them in UnloadFont (called by
    // ManagedFont.Dispose) — so they MUST be allocated with raylib's own allocator (MemAlloc).
    private static unsafe Font BuildFont(ParsedFontFile parsedFontFile, Texture2D pageTexture, int[]? pageYOffsets = null)
    {
        // ParsedFontFile's constructor already throws when either line is missing; checked here,
        // before MemAlloc, so a violation can't leak the native arrays.
        FontFileInfoLine info = parsedFontFile.Info
            ?? throw new InvalidOperationException("Font file did not have an info tag");
        FontFileCommonLine common = parsedFontFile.Common
            ?? throw new InvalidOperationException("Font file did not have a common tag");

        int glyphCount = parsedFontFile.Chars.Count;

        Rectangle* recs = (Rectangle*)MemAlloc((uint)(glyphCount * sizeof(Rectangle)));
        GlyphInfo* glyphs = (GlyphInfo*)MemAlloc((uint)(glyphCount * sizeof(GlyphInfo)));

        for (int i = 0; i < glyphCount; i++)
        {
            FontFileCharLine charLine = parsedFontFile.Chars[i];
            int recY = charLine.Y + (pageYOffsets != null ? pageYOffsets[charLine.Page] : 0);
            recs[i] = new Rectangle(charLine.X, recY, charLine.Width, charLine.Height);
            // Image is left default — raylib's DrawTextPro renders glyphs from Recs + the atlas
            // Texture, not from per-glyph Images.
            glyphs[i] = new GlyphInfo
            {
                Value = charLine.Id,
                OffsetX = charLine.XOffset,
                OffsetY = charLine.YOffset,
                AdvanceX = charLine.XAdvance,
            };
        }

        Font font = new Font
        {
            BaseSize = info.Size,
            GlyphCount = glyphCount,
            GlyphPadding = 0,
            Texture = pageTexture,
            Recs = recs,
            Glyphs = glyphs,
        };

        // raylib's Font has no lineHeight/base field, so record the .fnt's values keyed by the atlas
        // texture id. The Text renderable uses these for line height and descender so raylib matches
        // the MonoGame BitmapFont; without it, line height collapses to BaseSize (no descender region).
        RaylibFontMetricsRegistry.Register(pageTexture.Id, common.LineHeight, common.Base);

        // Apply the project's texture filter (#3496) once, here, since every bitmap-font
        // construction path (BuildBitmapFont, KernSmith's BuildFontFromFntText)
        // funnels through BuildFont. Bitmap font atlases pack glyphs edge-to-edge with little/no
        // padding, so Linear filtering can bleed adjacent glyphs' pixels at the seams — an inherent
        // tradeoff of the project's chosen filter, already present identically on MonoGame.
        TextureFilterApplier(pageTexture, DefaultTextureFilter);

        return font;
    }

    // #4057: loads the "-shadow.fnt" sibling next to primaryFntPath (if present) and records it in
    // RaylibFontShadowRegistry against the primary's texture id. Absent for the vast majority of
    // fonts (no dropshadow requested), in which case this is a no-op - the Text renderable's
    // shadow-registry lookup simply finds nothing and draws no shadow pass. Resolved the same way as
    // the primary (#5253). The shadow always gets its own page texture, because
    // ManagedFont.Dispose unloads both fonts' textures.
    private static void RegisterShadowSiblingIfPresent(string primaryFntPath, uint primaryTextureId)
    {
        const string fntExtension = ".fnt";
        if (!primaryFntPath.EndsWith(fntExtension, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string shadowFntPath = primaryFntPath.Substring(0, primaryFntPath.Length - fntExtension.Length)
            + "-shadow" + fntExtension;

        Font? shadowFont = null;
        try
        {
            // Same order as the primary: the hook, then a loose file, then FileManager's other
            // disk fallbacks (#5299).
            shadowFont = TryLoadBitmapFont(shadowFntPath, registerShadowSibling: false);
        }
        catch (Exception exception)
        {
            // The shadow is optional: a broken sibling costs the shadow, not the primary font.
            Console.Error.WriteLine($"Could not load dropshadow font '{shadowFntPath}': {exception.Message}");
        }

        if (shadowFont != null)
        {
            RaylibFontShadowRegistry.Register(primaryTextureId, shadowFont.Value);
        }
    }

    public static string StandardizeCaseSensitive(string fileName)
    {
        const bool preserveCase = true;

        string fileNameStandardized = FileManager.Standardize(fileName, preserveCase, false);

        if (FileManager.IsRelative(fileNameStandardized) && FileManager.IsUrl(fileName) == false)
        {
            fileNameStandardized = FileManager.RelativeDirectory + fileNameStandardized;

            fileNameStandardized = FileManager.RemoveDotDotSlash(fileNameStandardized);
        }

        return fileNameStandardized;
    }

    string GetSystemFontPath(string fontFileName)
    {
        if(fontFileName.EndsWith(".ttf") == false)
        {
            fontFileName = fontFileName + ".ttf";
        }

        var directory =
            OperatingSystem.IsWindows() ? "C:/Windows/Fonts"
            : OperatingSystem.IsLinux() ? "/usr/share/fonts/truetype"
            : OperatingSystem.IsMacOS() ? "/System/Library/Fonts"
            : string.Empty;

        // first check no-space since that's what Windows does:
        var noSpace = Path.Combine(directory, fontFileName.Replace(" ", ""));
        if(System.IO.File.Exists(noSpace))
        {
            return noSpace;
        }
        else
        {
            return Path.Combine(directory, fontFileName);
        }
    }

    /// <inheritdoc/>
    public T? TryLoadContent<T>(string contentName)
    {
        if (typeof(T) == typeof(Texture2D))
        {
            // Same hook-honoring path as LoadContent, but "Try" swallows load failures and returns
            // default rather than propagating the IOException GetStreamForFile throws when missing.
            try
            {
                string fileNameStandardized = StandardizeCaseSensitive(contentName);
                return (T)(object)LoadTextureFromFile(fileNameStandardized);
            }
            catch
            {
                return default(T);
            }
        }
        else
        {
            return default(T);
        }
    }
}
