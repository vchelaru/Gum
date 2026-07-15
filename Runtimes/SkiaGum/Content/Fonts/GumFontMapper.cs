using RenderingLibrary.Graphics.Fonts;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
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
        if (style.FontFamily != null && registry.TryGetValue(style.FontFamily, out SKTypeface? typeface))
        {
            return typeface;
        }

        return base.TypefaceFromStyle(style, ignoreFontVariants);
    }
}
