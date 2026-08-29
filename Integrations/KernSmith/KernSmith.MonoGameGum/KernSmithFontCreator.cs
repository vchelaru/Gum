using KernSmith.Atlas;
using KernSmith.Output;
using KernSmith.Output.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace KernSmith.Gum;

/// <summary>
/// Creates <see cref="BitmapFont"/> instances in memory using KernSmith for Gum games.
/// Generates font textures and metadata without any disk I/O.
/// </summary>
public class KernSmithFontCreator : IInMemoryFontCreator, IGrowableFontCreator
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly RasterizerBackend? _backend;

    // Growth bookkeeping (issue #4535 Phase 2), keyed by BmfcSave.FontCacheFileName -- the same
    // identity TryCreateFont already names its textures after. Populated by TryCreateFont so
    // TryAddGlyphs can resume growth against the exact font this creator generated. A font with no
    // FontFile (a system font) never gets an entry: incremental sessions require font file bytes.
    private readonly Dictionary<string, BmFontModel> _growableModels = new();

    // Sessions are created lazily on first TryAddGlyphs call per font identity, not eagerly in
    // TryCreateFont -- matches KernSmith's documented usage (create on first miss, keep for the
    // rest of the run) and avoids paying a session's rasterizer/parse cost for fonts that never grow.
    private readonly Dictionary<string, BmFontIncrementalSession> _activeSessions = new();

    /// <summary>
    /// Initializes a new instance of <see cref="KernSmithFontCreator"/>.
    /// </summary>
    /// <param name="graphicsDevice">
    /// The graphics device used to create font textures.
    /// </param>
    /// <param name="backend">
    /// Optional rasterizer backend override. When null, uses the default (FreeType).
    /// Use <see cref="RasterizerBackend.StbTrueType"/> on platforms where native
    /// libraries are unavailable (e.g., Blazor WASM).
    /// </param>
    public KernSmithFontCreator(GraphicsDevice graphicsDevice, RasterizerBackend? backend = null)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _backend = backend;
    }

    /// <summary>
    /// Registers raw font data (TTF/OTF/WOFF) under a family name so that
    /// font generation can resolve it without accessing system fonts.
    /// </summary>
    /// <param name="familyName">Font family name (e.g., "Arial").</param>
    /// <param name="fontData">Raw font file bytes.</param>
    /// <param name="style">
    /// Optional style name (e.g., "Bold", "Italic", "Bold Italic").
    /// When null, registers as the default/regular variant.
    /// </param>
    /// <param name="faceIndex">TTC face index (0 for single-face font files).</param>
    public static void RegisterFont(string familyName, byte[] fontData, string? style = null, int faceIndex = 0)
        => BmFont.RegisterFont(familyName, fontData, style, faceIndex);

    /// <summary>
    /// Registers a font file under a family name, reading it through
    /// <see cref="FileManager.GetStreamForFile"/> so a font that only exists behind a host stream
    /// hook (a .ttf packed into a .gumpkg, or one in a game's asset zip) registers the same as one
    /// on disk, and otherwise via TitleContainer.OpenStream, which resolves content files correctly
    /// on all platforms (desktop, Android, iOS, consoles).
    /// </summary>
    /// <param name="familyName">Font family name (e.g., "Arial").</param>
    /// <param name="filePath">
    /// Path to a .ttf, .otf, or .woff font file, relative to <see cref="FileManager.RelativeDirectory"/>
    /// - the same base a <c>Font</c>/<c>CustomFontFile</c> path uses (#4527).
    /// </param>
    /// <param name="style">
    /// Optional style name (e.g., "Bold", "Italic", "Bold Italic").
    /// When null, registers as the default/regular variant.
    /// </param>
    /// <param name="faceIndex">TTC face index (0 for single-face font files).</param>
    public static void RegisterFont(string familyName, string filePath,
        string? style = null, int faceIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(familyName);
        ArgumentNullException.ThrowIfNull(filePath);

        BmFont.RegisterFont(familyName, ReadFontBytes(filePath), style, faceIndex);
    }

    /// <summary>
    /// Resolves <paramref name="filePath"/> from <see cref="FileManager.RelativeDirectory"/> via
    /// <see cref="GumFontGenerator.ReadFontFileBytes"/> - the stream hook when one is installed,
    /// otherwise disk - the same base and resolution order <c>Font</c>/<c>CustomFontFile</c> use.
    /// Falls back to <see cref="TitleContainer"/>, which knows where content lives on platforms
    /// plain file I/O can't reach (Android, iOS, consoles); the fallback path is translated to stay
    /// relative to <see cref="FileManager.RelativeDirectory"/> too, so both routes accept the same
    /// string for the same file (#4527). The translation only applies when RelativeDirectory sits
    /// under the executable directory (the common desktop case); elsewhere <paramref name="filePath"/>
    /// is handed to TitleContainer unchanged, matching prior behavior.
    /// </summary>
    private static byte[] ReadFontBytes(string filePath)
    {
        try
        {
            return GumFontGenerator.ReadFontFileBytes(filePath);
        }
        catch (IOException)
        {
            // Not behind the hook and not on disk at the FileManager.RelativeDirectory-resolved
            // path; fall through to the title container below.
        }

        string titleContainerPath = filePath;
        if (FileManager.IsRelative(filePath))
        {
            string absolutePath = FileManager.MakeAbsolute(filePath);
            string exeLocation = FileManager.ExeLocation;
            if (absolutePath.StartsWith(exeLocation, StringComparison.OrdinalIgnoreCase))
            {
                titleContainerPath = absolutePath.Substring(exeLocation.Length).Replace('\\', '/');
            }
        }

        using Stream stream = TitleContainer.OpenStream(titleContainerPath);
        using MemoryStream memoryStream = new();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Removes a previously registered font.
    /// </summary>
    /// <param name="familyName">Font family name.</param>
    /// <param name="style">Optional style name, or null for the default variant.</param>
    /// <returns>True if a font was removed.</returns>
    public static bool UnregisterFont(string familyName, string? style = null)
        => BmFont.UnregisterFont(familyName, style);

    /// <summary>
    /// Removes all registered fonts.
    /// </summary>
    public static void ClearRegisteredFonts()
        => BmFont.ClearRegisteredFonts();

    /// <inheritdoc/>
    public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
    {
        BmFontResult result = GumFontGenerator.Generate(bmfcSave, _backend);

        string baseName = System.IO.Path.GetFileNameWithoutExtension(bmfcSave.FontCacheFileName);

        Texture2D[] textures = new Texture2D[result.Pages.Count];
        for (int i = 0; i < result.Pages.Count; i++)
        {
            AtlasPage page = result.Pages[i];
            Texture2D texture = new Texture2D(_graphicsDevice, page.Width, page.Height,
                false, SurfaceFormat.Color);
            texture.Name = $"{baseName}_{i}";
            texture.SetData(page.PixelData);
            textures[i] = texture;
        }

        BitmapFont bitmapFont = new BitmapFont(textures, result.FntText);

        // Issue #4061: attach the shadow AtlasVariant (if requested) as ShadowFont. It shares the
        // primary's atlas pages (same shared texture array), so no extra texture upload is needed.
        if (result.VariantModels.ContainsKey("shadow"))
        {
            bitmapFont.ShadowFont = new BitmapFont(textures, result.GetVariantFntText("shadow"));
        }

        // Issue #4535: remember this font's model so a later TryAddGlyphs call can resume an
        // incremental session against it. Dropshadow fonts (Variants set) are skipped -- KernSmith's
        // incremental sessions reject Variants outright (ValidateIncrementalOptions), so there is
        // nothing growable to remember for them.
        if (!string.IsNullOrEmpty(bmfcSave.FontFile) && !bmfcSave.HasDropshadow)
        {
            _growableModels[bmfcSave.FontCacheFileName] = result.Model;
        }

        return bitmapFont;
    }

    /// <summary>
    /// Adds <paramref name="characters"/> to <paramref name="font"/>, a font this creator previously
    /// built via <see cref="TryCreateFont"/>, growing its live atlas texture(s) in place (KernSmith
    /// 0.21.0 incremental sessions, issue #4535) instead of a full regenerate.
    /// </summary>
    /// <param name="font">The font to grow, as returned by an earlier <see cref="TryCreateFont"/> call.</param>
    /// <param name="bmfcSave">
    /// The same descriptor <paramref name="font"/> was created from. Must match -- padding, spacing,
    /// size and outline are verified against the font's original generation and a mismatch throws.
    /// </param>
    /// <param name="characters">The characters to add. Already-present characters are a no-op; codepoints the font cannot render are reported in the result, not thrown.</param>
    /// <returns>
    /// The addition result, or null when <paramref name="font"/> was not created by this creator's
    /// <see cref="TryCreateFont"/> (or was generated from a system font / with a dropshadow, neither
    /// of which this creator tracks as growable).
    /// </returns>
    public GlyphAdditionResult? TryAddGlyphs(BitmapFont font, BmfcSave bmfcSave, string characters)
    {
        string key = bmfcSave.FontCacheFileName;
        if (!_growableModels.TryGetValue(key, out BmFontModel? model))
        {
            return null;
        }

        if (!_activeSessions.TryGetValue(key, out BmFontIncrementalSession? session))
        {
            session = GumFontGenerator.ResumeIncremental(bmfcSave, model, backend: _backend);
            _activeSessions[key] = session;
        }

        GlyphAdditionResult result = session.AddGlyphs(characters);

        if (result.Added.Count > 0)
        {
            ApplyGrowth(font, result);
        }

        return result;
    }

    /// <summary>
    /// <see cref="IGrowableFontCreator"/> adapter over <see cref="TryAddGlyphs(BitmapFont, BmfcSave, string)"/>
    /// (issue #4542) -- converts the richer KernSmith <see cref="GlyphAdditionResult"/> down to the
    /// interface's plain list of characters this font cannot render, so TextRuntime's automatic
    /// growth trigger (RenderingLibrary/MonoGameGum) can call it without depending on this optional
    /// KernSmith package's types.
    /// </summary>
    IReadOnlyList<char>? IGrowableFontCreator.TryAddGlyphs(BitmapFont font, BmfcSave bmfcSave, string characters)
    {
        GlyphAdditionResult? result = TryAddGlyphs(font, bmfcSave, characters);
        if (result == null)
        {
            return null;
        }

        return result.FailedCodepoints.Select(cp => (char)cp).ToArray();
    }

    private void ApplyGrowth(BitmapFont font, GlyphAdditionResult result)
    {
        if (result.PageGrown || result.PageCount > font.Textures.Length)
        {
            GrowTexturePages(font, result);
        }

        foreach (AddedGlyph glyph in result.Added)
        {
            Texture2D page = font.Textures[glyph.PageIndex];
            Rectangle destRect = new Rectangle(glyph.X, glyph.Y, glyph.Width, glyph.Height);
            // Pixels is tightly-packed straight-alpha RGBA32 -- reinterpret as Color[] rather than
            // handing SetData the raw byte[] (which would need elementCount in bytes, not pixels).
            Color[] glyphPixels = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, Color>(glyph.Pixels).ToArray();
            page.SetData(0, destRect, glyphPixels, 0, glyph.Width * glyph.Height);

            font.AddOrUpdateCharacter(ToFontFileCharLine(glyph.Char), page.Width, page.Height);
        }

        foreach (KerningEntry kerning in result.NewKerning)
        {
            font.AddKerningPair(kerning.First, kerning.Second, kerning.Amount);
        }
    }

    /// <summary>
    /// Reallocates <paramref name="font"/>'s texture pages to match <paramref name="result"/>'s
    /// current page geometry. A width/height change (<see cref="GlyphAdditionResult.PageGrown"/>)
    /// replaces every page (copying old pixel content to (0,0), per KernSmith's grow contract) and
    /// rescales every already-placed character's UVs against the new size, since their pixel
    /// positions do not move but the denominator they were computed against did. A page-count-only
    /// change (<see cref="AdditionOverflowPolicy.NewPage"/>) reuses existing page instances untouched
    /// and only appends new, empty pages.
    /// </summary>
    private void GrowTexturePages(BitmapFont font, GlyphAdditionResult result)
    {
        Texture2D[] oldTextures = font.Textures;
        Texture2D[] newTextures = new Texture2D[result.PageCount];

        for (int i = 0; i < result.PageCount; i++)
        {
            if (!result.PageGrown && i < oldTextures.Length)
            {
                newTextures[i] = oldTextures[i];
                continue;
            }

            Texture2D newPage = new Texture2D(_graphicsDevice, result.PageWidth, result.PageHeight,
                false, SurfaceFormat.Color);
            newPage.Name = i < oldTextures.Length ? oldTextures[i].Name : $"{oldTextures[0].Name}_{i}";

            if (i < oldTextures.Length)
            {
                Texture2D oldPage = oldTextures[i];
                Color[] oldPixels = new Color[oldPage.Width * oldPage.Height];
                oldPage.GetData(oldPixels);
                newPage.SetData(0, new Rectangle(0, 0, oldPage.Width, oldPage.Height), oldPixels, 0, oldPixels.Length);
            }

            newTextures[i] = newPage;
        }

        if (result.PageGrown)
        {
            foreach (Texture2D oldPage in oldTextures)
            {
                oldPage?.Dispose();
            }
            font.RescaleTextureCoordinates(result.PageWidth, result.PageHeight);
        }

        font.ReplaceTexturePages(newTextures);
    }

    private static FontFileCharLine ToFontFileCharLine(CharEntry charEntry) => new FontFileCharLine
    {
        Id = charEntry.Id,
        X = charEntry.X,
        Y = charEntry.Y,
        Width = charEntry.Width,
        Height = charEntry.Height,
        XOffset = charEntry.XOffset,
        YOffset = charEntry.YOffset,
        XAdvance = charEntry.XAdvance,
        Page = charEntry.Page,
    };
}
