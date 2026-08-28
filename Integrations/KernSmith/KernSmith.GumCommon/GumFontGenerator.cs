using KernSmith;
using KernSmith.Output;
using KernSmith.Rasterizer;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace KernSmith.Gum;

/// <summary>
/// Bridges Gum's <see cref="BmfcSave"/> font descriptor with KernSmith's font generation
/// pipeline. Used by platform-specific packages (KernSmith.MonoGameGum, KernSmith.KniGum, etc.)
/// to generate bitmap fonts at runtime without duplicating the mapping logic.
/// </summary>
public static class GumFontGenerator
{
    /// <summary>
    /// Generates a <see cref="BmFontResult"/> from a Gum <see cref="BmfcSave"/> font descriptor.
    /// The result contains .fnt metadata and texture page pixel data entirely in memory.
    /// </summary>
    /// <param name="bmfcSave">The Gum font descriptor to generate from.</param>
    /// <param name="backend">
    /// Optional rasterizer backend override. When null, uses the default (FreeType).
    /// Use <see cref="RasterizerBackend.StbTrueType"/> on platforms where native
    /// libraries are unavailable (e.g., Blazor WASM).
    /// </param>
    public static BmFontResult Generate(BmfcSave bmfcSave, RasterizerBackend? backend = null)
    {
        FontGeneratorOptions options = BuildOptions(bmfcSave);
        if (backend.HasValue)
            options.Backend = backend.Value;
        return string.IsNullOrEmpty(bmfcSave.FontFile)
            ? BmFont.GenerateFromSystem(bmfcSave.FontName, options)
            : BmFont.Generate(ReadFontFileBytes(bmfcSave.FontFile!), options);
    }

    /// <summary>
    /// Reads a font file through <see cref="FileManager.GetStreamForFile"/> instead of handing its
    /// path to KernSmith, so a font that only exists behind a host stream hook — a <c>.ttf</c>
    /// packed into a <c>.gumpkg</c>, or one in a game's asset zip — rasterizes the same as one on
    /// disk (#4515). Falls back to disk because FileManager routes exclusively to the hook once one
    /// is installed, and a hook that doesn't carry this font must not hide a copy on disk.
    /// Called by the platform packages' font registration as well as by generation.
    /// </summary>
    public static byte[] ReadFontFileBytes(string fontFile)
    {
        string fullPath = FileManager.IsRelative(fontFile) ? FileManager.MakeAbsolute(fontFile) : fontFile;

        try
        {
            using Stream stream = FileManager.GetStreamForFile(fullPath);
            using MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
        catch (IOException) when (File.Exists(fullPath))
        {
            return File.ReadAllBytes(fullPath);
        }
    }

    /// <summary>
    /// Maps a Gum <see cref="BmfcSave"/> to KernSmith <see cref="FontGeneratorOptions"/>.
    /// Exposed publicly so callers can inspect or customize options before generating.
    /// </summary>
    public static FontGeneratorOptions BuildOptions(BmfcSave bmfcSave)
    {
        FontGeneratorOptions options = new FontGeneratorOptions();

        options.Size = bmfcSave.FontSize;
        // Gum always uses "match character height" mode (negative fontSize in .bmfc),
        // which scales the font so the tallest glyph matches the requested pixel size.
        options.MatchCharHeight = true;
        options.Bold = bmfcSave.IsBold;
        options.Italic = bmfcSave.IsItalic;
        options.AntiAlias = bmfcSave.UseSmoothing ? AntiAliasMode.Grayscale : AntiAliasMode.None;
        options.Outline = bmfcSave.OutlineThickness;
        options.Spacing = new Spacing(bmfcSave.SpacingHorizontal, bmfcSave.SpacingVertical);
        options.MaxTextureWidth = bmfcSave.OutputWidth;
        options.MaxTextureHeight = bmfcSave.OutputHeight;

        ApplyChannelLayout(bmfcSave, options);

        List<int> codepoints = ParseCharRanges(bmfcSave.Ranges);
        options.Characters = CharacterSet.FromChars(codepoints);

        ApplyShadowOptions(bmfcSave, options);

        return options;
    }

    /// <summary>
    /// Selects BMFont channel layout for Gum's text renderer, or leaves channels at the KernSmith
    /// default when baked effects need full RGBA preserved.
    /// </summary>
    private static void ApplyChannelLayout(BmfcSave bmfcSave, FontGeneratorOptions options)
    {
        // Issue #4001: drop shadow is a ShadowSilhouette atlas variant (see ApplyShadowOptions), and
        // KernSmith forbids a custom ChannelConfig alongside Variants. Leave Channels at the default
        // glyph-coverage layout so the primary stays a plain glyph atlas the runtime can tint on its
        // own, with the shadow drawn separately underneath.
        if (bmfcSave.HasDropshadow)
        {
            return;
        }

        // Match bmfont.exe channel layout so Gum's runtime renders correctly.
        // No outline: alpha=glyph shape, RGB=white (One). Glyph is white text with alpha transparency.
        // With outline: alpha=outline, RGB=glyph. Outline uses color channels.
        if (bmfcSave.OutlineThickness == 0)
        {
            options.Channels = new ChannelConfig(
                Alpha: ChannelContent.Glyph,
                Red: ChannelContent.One,
                Green: ChannelContent.One,
                Blue: ChannelContent.One);
        }
        else
        {
            options.Channels = new ChannelConfig(
                Alpha: ChannelContent.Outline,
                Red: ChannelContent.Glyph,
                Green: ChannelContent.Glyph,
                Blue: ChannelContent.Glyph);
        }
    }

    private static void ApplyShadowOptions(BmfcSave bmfcSave, FontGeneratorOptions options)
    {
        if (!bmfcSave.HasDropshadow)
        {
            return;
        }

        // Issue #4001: request a ShadowSilhouette variant packed into the same shared atlas as the
        // primary glyphs, instead of baking a colored shadow into the primary. Offset and color are
        // applied by the runtime at draw time, so BlurRadius is the only shadow parameter that
        // affects the baked silhouette shape.
        options.Variants = new[]
        {
            new AtlasVariant("shadow", AtlasVariantKind.ShadowSilhouette,
                BlurRadius: (int)MathF.Round(bmfcSave.DropshadowBlur)),
        };
    }

    /// <summary>
    /// Parses a BMFont-style character range string (e.g. "32-126,160-255") into a list of
    /// individual codepoints. Duplicated from BmfcSave to avoid depending on a version that
    /// may not yet include this method.
    /// </summary>
    private static List<int> ParseCharRanges(string charsStr)
    {
        List<int> allChars = new List<int>();
        string[] ranges = charsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in ranges)
        {
            if (part.Contains('-'))
            {
                string[] split = part.Split('-');
                if (int.TryParse(split[0], out int start) && int.TryParse(split[1], out int end))
                {
                    for (int i = start; i <= end; i++)
                    {
                        allChars.Add(i);
                    }
                }
            }
            else if (int.TryParse(part, out int value))
            {
                allChars.Add(value);
            }
        }
        return allChars;
    }
}
