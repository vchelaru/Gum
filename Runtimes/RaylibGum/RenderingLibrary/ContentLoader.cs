using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
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
        // The CustomGetStreamFromFile hook wins over a loose file at the same path, as it does for
        // every other content load, so a loaded bundle overrides stale loose copies (#5299).
        string? hookedFntText = isFnt ? TryReadFntFromStreamHook(contentName) : null;
        if (hookedFntText != null)
        {
            font = BuildBitmapFontWithShadowSibling(contentName, hookedFntText);
        }
        else if (System.IO.File.Exists(contentName))
        {
            if (isFnt)
            {
                Font loadedFont = Raylib.LoadFont(contentName);
                // Apply the project's texture filter (#3496); raylib defaults new textures to point
                // filtering. Bitmap font atlases pack glyphs edge-to-edge with little/no padding, so
                // Linear filtering can bleed adjacent glyphs' pixels at the seams — an inherent
                // tradeoff of the project's chosen filter, already present identically on MonoGame.
                TextureFilterApplier(loadedFont.Texture, DefaultTextureFilter);
                font = loadedFont;
                // raylib's native loader discards the .fnt's lineHeight/base, so re-parse the on-disk
                // file to record them (same registry the in-memory/KernSmith path populates via
                // BuildFont). Keyed by atlas texture id so the Text renderable can recover them.
                RegisterFontMetricsFromFnt(File.ReadAllText(contentName), loadedFont.Texture.Id);
                // #4057: if a "-shadow.fnt" sibling exists (written by the font generator for a
                // dropshadow font, sharing the same PNG - see BitmapFont.LoadShadowSiblingIfPresent on
                // the MonoGame side), load it too and record it against the primary's texture id.
                RegisterShadowSiblingIfPresent(contentName, loadedFont.Texture.Id);
            }
            else
            {
                font = LoadFontEx(contentName, 24, null, 0);
                TextureFilterApplier(font.Value.Texture, DefaultTextureFilter);
            }
        }
        else if (isFnt)
        {
            // Neither the hook nor this exact disk path has the .fnt, but FileManager's own disk
            // fallback may (e.g. a macOS .app's Resources folder). Null falls through to the
            // default(Font) handling below. (#3037)
            font = TryLoadBitmapFontThroughStreamHook(contentName);
        }

        if (isFnt && font == null)
        {
            // If we got here, but we have an FNT file, then we should just return null:
            font = default(Font);
        }

        if (System.IO.File.Exists(contentName + ".ttf") && font == null)
        {
            font = LoadFontEx(contentName, 24, null, 0);
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

    // Loads an AngelCode bitmap font (.fnt + .png page) through FileManager.GetStreamForFile so the
    // CustomGetStreamFromFile hook and FileManager's disk fallbacks are honored. Returns null when
    // nothing can supply the .fnt, letting the caller fall back to default(Font). #3037
    private static Font? TryLoadBitmapFontThroughStreamHook(string fntPath)
    {
        string fntContents;
        try
        {
            fntContents = FileManager.FromFileText(fntPath);
        }
        catch
        {
            // No hook, or neither the hook nor disk can supply this .fnt — fall back.
            return null;
        }

        return BuildBitmapFontWithShadowSibling(fntPath, fntContents);
    }

    // raylib's path-based LoadFont can't see the hook, and LoadFontFromMemory can't resolve a bitmap
    // font's separate .png page, so parse the .fnt ourselves and load the page through the hooked
    // texture path.
    private static Font? BuildBitmapFontWithShadowSibling(string fntPath, string fntContents)
    {
        Font? font = BuildBitmapFontThroughStreamHook(fntPath, fntContents);
        if (font != null)
        {
            // #5253: a bundled dropshadow font ships its "-shadow.fnt" sibling in the same bundle.
            RegisterShadowSiblingIfPresent(fntPath, font.Value.Texture.Id);
        }
        return font;
    }

    // Reads a .fnt's text from CustomGetStreamFromFile alone, skipping GetStreamForFile's disk
    // fallback, so a caller can prefer the hook and still load loose files with raylib's native
    // loader. Returns null when no hook is installed, it misses, or it serves a multi-page font that
    // also exists on disk: only the native loader can merge pages, so the loose copy is used.
    private static string? TryReadFntFromStreamHook(string fntPath)
    {
        string? text = TryReadTextFromStreamHook(fntPath);
        if (text != null && System.IO.File.Exists(fntPath) && HasMultiplePages(text))
        {
            return null;
        }
        return text;
    }

    private static bool HasMultiplePages(string fntText)
    {
        try
        {
            return new ParsedFontFile(fntText).GetPagesAsArrayOfStrings.Length > 1;
        }
        catch
        {
            // Malformed text is reported by the load that follows, not here.
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

    // Parses fntContents and loads its page through the hooked texture path. Does not probe for a
    // shadow sibling, so loading the sibling itself through here cannot recurse.
    private static Font? BuildBitmapFontThroughStreamHook(string fntPath, string fntContents)
    {
        ParsedFontFile parsedFontFile = new ParsedFontFile(fntContents);

        string[] pageFileNames = parsedFontFile.GetPagesAsArrayOfStrings;
        if (pageFileNames.Length == 0)
        {
            return null;
        }

        // Multi-page fonts loaded through this hand-rolled path are a rare edge case (bundled AND
        // multi-page) that Gum has decided not to support — this is the intended terminal state, not
        // a TODO. raylib's native LoadFont merges multi-page .fnt atlases into one stacked texture
        // internally when loading straight off disk, but that native loader can't be used here (it
        // does its own file I/O with no concept of Gum's stream hook), and silently using only page 0
        // would mis-map every glyph on page 1+ against the wrong atlas texture with no error. #3496
        if (pageFileNames.Length > 1)
        {
            throw new NotSupportedException(
                $"Multi-page bitmap font '{fntPath}' has {pageFileNames.Length} pages, but multi-page " +
                "fonts loaded through a custom stream (bundle/zip/in-memory asset) are not supported. " +
                "Only single-page .fnt atlases work through this path; multi-page fonts work fine when " +
                "loaded as plain on-disk files, where raylib merges pages natively.");
        }

        // Gum's FontCache fonts are single-page, and raylib's Font has a single atlas texture, so
        // the first page is the atlas. The page path is relative to the .fnt; load it through the
        // already-hooked texture path so it resolves from the same bundle/stream as the .fnt.
        string pagePath = FileManager.GetDirectory(fntPath) + pageFileNames[0];
        Texture2D pageTexture = LoadTextureFromFile(pagePath);

        return BuildFont(parsedFontFile, pageTexture);
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
        // construction path (TryLoadBitmapFontThroughStreamHook, KernSmith's BuildFontFromFntText)
        // funnels through BuildFont. Bitmap font atlases pack glyphs edge-to-edge with little/no
        // padding, so Linear filtering can bleed adjacent glyphs' pixels at the seams — an inherent
        // tradeoff of the project's chosen filter, already present identically on MonoGame.
        TextureFilterApplier(pageTexture, DefaultTextureFilter);

        return font;
    }

    // Parses just the lineHeight/base out of a .fnt's text and records them against the loaded font's
    // atlas texture id. Used by the native on-disk path, which loads through raylib's own .fnt loader
    // (so it never goes through BuildFont). A malformed .fnt simply skips registration — the Text
    // renderable then falls back to native measurement.
    private static void RegisterFontMetricsFromFnt(string fntText, uint textureId)
    {
        try
        {
            ParsedFontFile parsedFontFile = new ParsedFontFile(fntText);
            if (parsedFontFile.Common != null)
            {
                RaylibFontMetricsRegistry.Register(textureId, parsedFontFile.Common.LineHeight, parsedFontFile.Common.Base);
            }
        }
        catch
        {
            // Leave unregistered; line height falls back to MeasureTextEx in the Text renderable.
        }
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
            string? hookedShadowText = TryReadFntFromStreamHook(shadowFntPath);
            if (hookedShadowText != null)
            {
                shadowFont = BuildBitmapFontThroughStreamHook(shadowFntPath, hookedShadowText);
            }
            else if (System.IO.File.Exists(shadowFntPath))
            {
                Font loadedShadowFont = Raylib.LoadFont(shadowFntPath);
                TextureFilterApplier(loadedShadowFont.Texture, DefaultTextureFilter);
                shadowFont = loadedShadowFont;
            }
            else if (FileManager.FileExists(shadowFntPath))
            {
                shadowFont = BuildBitmapFontThroughStreamHook(shadowFntPath, FileManager.FromFileText(shadowFntPath));
            }
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
