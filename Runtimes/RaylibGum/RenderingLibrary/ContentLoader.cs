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
public class ContentLoader : IContentLoader
{
    /// <inheritdoc/>
    public T LoadContent<T>(string contentName)
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
        // try loading locally first:
        if (System.IO.File.Exists(contentName))
        {
            if (isFnt)
            {
                font = Raylib.LoadFont(contentName);
            }
            else
            {
                font = LoadFontEx(contentName, 24, null, 0);
            }
        }
        else if (isFnt)
        {
            // The .fnt isn't on disk at this path, but it may still be reachable through the
            // FileManager.CustomGetStreamFromFile hook (e.g. a .gumpkg/zip bundle, the
            // GumFromZipFile sample, an encrypted/in-memory asset store). raylib's path-based
            // LoadFont can't see the hook, and LoadFontFromMemory can't resolve a bitmap font's
            // separate .png page from memory — so parse the .fnt ourselves (reusing
            // ParsedFontFile), load the page through the already-hooked texture path, and assemble
            // the raylib Font. Returns null if the hook can't supply the file, in which case we
            // fall through to the default(Font) handling below. (#3037)
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
        }

        if(font == null)
        {
            var systemFontPath = GetSystemFontPath(contentName);
            if (File.Exists(systemFontPath))
            {
                font = LoadFontEx(systemFontPath, 24, null, 0);
            }
            else
            {
                font = default(Font);
            }
        }

        if (LoaderManager.Self.CacheTextures && font != null)
        {
            var managedFont = new ManagedFont(font.Value);

            LoaderManager.Self.AddDisposable(contentNameStandardized, managedFont);
        }


        return font;
    }

    private static Texture2D? LoadTexture2D(string fileName)
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



        Texture2D? toReturn = null;
        if (FileManager.IsUrl(fileName))
        {
            throw new NotImplementedException("Loading textures from URLs is not implemented yet.");
        }
        else
        {
            // Load via fileNameStandardized so a relative fileName is resolved against
            // FileManager.RelativeDirectory — the same prefix the cache lookup above used.
            // Previously this was just `fileName`, which meant callers relying on
            // RelativeDirectory (e.g. AnimationChainList.ToAnimationChainList loading per-frame
            // textures relative to the .achx's folder) silently got an empty Texture2D and
            // Sprite.Render early-returned on null Texture.
            toReturn = LoadTextureFromFile(fileNameStandardized);
        }

        if (LoaderManager.Self.CacheTextures && toReturn != null)
        {
            var managedTexture = new ManagedTexture(toReturn.Value);

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
        UnloadImage(image);
        return toReturn;
    }

    // Loads an AngelCode bitmap font (.fnt + .png page) through FileManager.GetStreamForFile so the
    // CustomGetStreamFromFile hook is honored. Used only when the .fnt is not present on disk — the
    // on-disk path still uses raylib's native LoadFont (see LoadFont). Returns null when the hook
    // (or disk fallback) can't supply the .fnt, letting the caller fall back to default(Font). #3037
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

        ParsedFontFile parsedFontFile = new ParsedFontFile(fntContents);

        string[] pageFileNames = parsedFontFile.GetPagesAsArrayOfStrings;
        if (pageFileNames.Length == 0)
        {
            return null;
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
    // (instead of BuildFont) so callers never name ParsedFontFile, which is compiled into BOTH
    // GumCommon and RaylibGum — referencing it across the assembly boundary is an ambiguous-type
    // (CS0433) error. See InternalsVisibleTo in Properties/AssemblyInfo.cs.
    internal static Font BuildFontFromFntText(string fntText, Texture2D pageTexture)
    {
        return BuildFont(new ParsedFontFile(fntText), pageTexture);
    }

    // Assembles a raylib Font from a parsed .fnt and its already-loaded atlas page. The Recs and
    // Glyphs arrays are handed to raylib, which frees them in UnloadFont (called by
    // ManagedFont.Dispose) — so they MUST be allocated with raylib's own allocator (MemAlloc).
    private static unsafe Font BuildFont(ParsedFontFile parsedFontFile, Texture2D pageTexture)
    {
        int glyphCount = parsedFontFile.Chars.Count;

        Rectangle* recs = (Rectangle*)MemAlloc((uint)(glyphCount * sizeof(Rectangle)));
        GlyphInfo* glyphs = (GlyphInfo*)MemAlloc((uint)(glyphCount * sizeof(GlyphInfo)));

        for (int i = 0; i < glyphCount; i++)
        {
            FontFileCharLine charLine = parsedFontFile.Chars[i];
            recs[i] = new Rectangle(charLine.X, charLine.Y, charLine.Width, charLine.Height);
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

        return new Font
        {
            BaseSize = parsedFontFile.Info.Size,
            GlyphCount = glyphCount,
            GlyphPadding = 0,
            Texture = pageTexture,
            Recs = recs,
            Glyphs = glyphs,
        };
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
    public T TryLoadContent<T>(string contentName)
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
