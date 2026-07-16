using RenderingLibrary.Graphics.Fonts;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Topten.RichTextKit;

namespace SkiaGum.Content.Fonts;

/// <summary>
/// Resolves <see cref="Style.FontFamily"/> to an <see cref="SKTypeface"/>, consulting a registry
/// of typefaces loaded from custom .ttf font files before falling back to RichTextKit's
/// default system-font resolution (<see cref="Topten.RichTextKit.FontMapper.TypefaceFromStyle"/>
/// -&gt; <see cref="SKTypeface.FromFamilyName(string?)"/>).
/// <para>
/// #3670/#3703: previously Skia had no custom-font-file loading path at all -- <c>Font</c> and
/// <c>CustomFontFile</c> were passed straight through as family-name strings, so pointing either
/// at a bundled .ttf silently resolved to nothing (or an unrelated system font of the same name).
/// This mirrors the XNA-like/raylib "font file" concept, but through SkiaSharp's native TTF
/// loading rather than a KernSmith/bmfont.exe-baked atlas -- Skia needs no atlas.
/// </para>
/// </summary>
public class GumFontMapper : FontMapper
{
    private static readonly ConcurrentDictionary<string, SKTypeface> registry =
        new ConcurrentDictionary<string, SKTypeface>(StringComparer.OrdinalIgnoreCase);

    private static readonly ConditionalWeakTable<SKTypeface, string> typefaceKeys = new();
    private static int nextTypefaceId;

    /// <summary>
    /// Embedded fonts registered via <see cref="RegisterFont"/>, keyed by family name + style slot
    /// (as opposed to <see cref="registry"/>, which is keyed by file path or typeface instance).
    /// </summary>
    private static readonly ConcurrentDictionary<string, SKTypeface> embeddedFontRegistry =
        new ConcurrentDictionary<string, SKTypeface>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Font weight above which <see cref="TypefaceFromStyle"/> treats a run as bold when resolving
    /// an <see cref="embeddedFontRegistry"/> style slot. 400 is <see cref="Style"/>'s regular-weight
    /// default; anything above it is a bold request, whether that's <c>TextRuntime.IsBold</c> on Skia
    /// (which maps to <c>Style.FontWeight</c> 600 via <c>BoldWeight</c> 1.5) or the <c>[IsBold]</c>
    /// BBCode tag (which sets <c>Style.FontWeight</c> 700 directly) -- a plain <c>&gt;= 700</c> check
    /// would miss the former.
    /// </summary>
    private const int RegularFontWeight = 400;

    /// <summary>
    /// Registers an already-loaded <see cref="SKTypeface"/> directly (as opposed to
    /// <see cref="RegisterFontFile"/>, which loads one from a path) and returns the family-name
    /// key to assign as <c>Style.FontFamily</c> to reference it -- used by <c>Text.Typeface</c>.
    /// Calling this again with the SAME instance returns the same key; a different instance --
    /// even one loaded from the same file -- gets its own key, since callers may swap in a
    /// distinct object at runtime and expect it to resolve independently.
    /// </summary>
    public static string RegisterTypeface(SKTypeface typeface)
    {
        if (typefaceKeys.TryGetValue(typeface, out string? existingKey))
        {
            return existingKey;
        }

        string key = $"__typeface_{Interlocked.Increment(ref nextTypefaceId)}";
        registry[key] = typeface;
        typefaceKeys.Add(typeface, key);
        return key;
    }

    /// <summary>
    /// Loads (or reuses an already-loaded) <see cref="SKTypeface"/> for <paramref name="fontFilePath"/>
    /// and returns the family-name key to assign as <c>Text.FontName</c> / <c>Style.FontFamily</c> to
    /// reference it. Returns null if the file doesn't exist or fails to parse as a font.
    /// </summary>
    public static string? RegisterFontFile(string fontFilePath)
    {
        string fullPath = ToolsUtilities.FileManager.Standardize(fontFilePath, preserveCase: true, makeAbsolute: true);

        if (registry.ContainsKey(fullPath))
        {
            return fullPath;
        }

        if (!ToolsUtilities.FileManager.FileExists(fullPath))
        {
            return null;
        }

        SKTypeface? typeface = SKTypeface.FromFile(fullPath);
        if (typeface == null)
        {
            return null;
        }

        registry[fullPath] = typeface;
        return fullPath;
    }

    /// <summary>
    /// Registers an in-memory TTF/OTF (<paramref name="fontData"/>) under <paramref name="familyName"/>
    /// plus an optional style slot ("Bold"/"Italic"/"BoldItalic", or null for the default cut) and
    /// returns the loaded <see cref="SKTypeface"/>, or null if the data doesn't parse as a font. Mirrors
    /// the KernSmith <c>RegisterFont(familyName, fontData, style)</c> surface XNA-like/raylib themes call
    /// through <c>Gum.Themes.ThemePlatform.RegisterFont</c>, so a theme's embedded fonts (which never
    /// touch disk) resolve on Skia through the same family+style vocabulary a theme already uses for
    /// every other backend, instead of the file-path/instance-only <see cref="RegisterFontFile"/> /
    /// <see cref="RegisterTypeface"/> (#3671). Unlike those two, callers keep addressing the font by
    /// <paramref name="familyName"/> -- <see cref="TypefaceFromStyle"/> picks the matching style slot
    /// from a run's bold/italic state.
    /// </summary>
    public static SKTypeface? RegisterFont(string familyName, byte[] fontData, string? style = null)
    {
        SKTypeface? typeface = SKTypeface.FromStream(new MemoryStream(fontData));
        if (typeface == null)
        {
            return null;
        }

        embeddedFontRegistry[GetEmbeddedFontKey(familyName, style)] = typeface;
        return typeface;
    }

    /// <summary>
    /// Resolves the family-name to assign as <c>Text.FontName</c> for the given TextRuntime font
    /// properties. <see cref="BmfcSave.ResolveTtfSourcePath"/> is the single backend-agnostic
    /// "which property is a font file" decision (also used by XNA-like/raylib's bake path), so
    /// Skia's and XNA-like's font-file detection can't drift out of sync (#3670/#3703). When it
    /// resolves a .ttf path, that file is registered here and its key returned. When
    /// <paramref name="useCustomFont"/> is set but <paramref name="customFontFile"/> isn't a .ttf
    /// (e.g. the XNA-like/raylib .fnt baked-atlas format, which Skia has no renderer for), returns
    /// null -- unsupported, not silently wrong. Otherwise returns <paramref name="font"/> unchanged
    /// (a plain system family name).
    /// </summary>
    public static string? ResolveFontFamily(bool useCustomFont, string? customFontFile, string? font)
    {
        string? ttfSourcePath = BmfcSave.ResolveTtfSourcePath(useCustomFont, customFontFile, font);

        if (ttfSourcePath != null)
        {
            return RegisterFontFile(ttfSourcePath);
        }

        return useCustomFont ? null : font;
    }

    /// <inheritdoc/>
    public override SKTypeface TypefaceFromStyle(IStyle style, bool ignoreFontVariants)
    {
        if (style.FontFamily != null)
        {
            string styleSlot = GetStyleSlot(isBold: style.FontWeight > RegularFontWeight, isItalic: style.FontItalic);
            if (embeddedFontRegistry.TryGetValue(GetEmbeddedFontKey(style.FontFamily, styleSlot), out SKTypeface? embeddedTypeface))
            {
                return embeddedTypeface;
            }

            // Requested style slot (e.g. Bold) has no registered cut -- fall back to the family's
            // default cut rather than dropping straight to a system font, so a theme that only
            // bundled one weight still renders with it instead of silently swapping typefaces.
            if (styleSlot.Length > 0
                && embeddedFontRegistry.TryGetValue(GetEmbeddedFontKey(style.FontFamily, null), out embeddedTypeface))
            {
                return embeddedTypeface;
            }

            if (registry.TryGetValue(style.FontFamily, out SKTypeface? typeface))
            {
                return typeface;
            }
        }

        return base.TypefaceFromStyle(style, ignoreFontVariants);
    }

    private static string GetEmbeddedFontKey(string familyName, string? style) =>
        $"{familyName} {style}";

    private static string GetStyleSlot(bool isBold, bool isItalic) => (isBold, isItalic) switch
    {
        (true, true) => "BoldItalic",
        (true, false) => "Bold",
        (false, true) => "Italic",
        (false, false) => string.Empty,
    };
}
