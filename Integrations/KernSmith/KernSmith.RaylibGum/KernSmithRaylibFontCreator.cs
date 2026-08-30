using KernSmith.Atlas;
using KernSmith.Output;
using KernSmith.Output.Model;
using KernSmith.Rasterizer;
using Raylib_cs;
using RaylibGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics.Fonts;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace KernSmith.Gum;

/// <summary>
/// Creates <see cref="Raylib_cs.Font"/> instances in memory using KernSmith for raylib + Gum games.
/// Rasterizes a font atlas with KernSmith and uploads it to a raylib texture — no .fnt files on disk.
/// </summary>
/// <remarks>
/// Wire up once after initializing Gum:
/// <code>CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();</code>
/// This is the raylib counterpart to KernSmith.MonoGameGum's <c>KernSmithFontCreator</c>.
/// </remarks>
public class KernSmithRaylibFontCreator : IRaylibFontCreator, IGrowableRaylibFontCreator
{
    /// <summary>
    /// Tracks a font identity this creator has grown at least once (issue #4546). <see cref="Raylib_cs.Font"/>
    /// is a value type -- unlike KernSmith.MonoGameGum's <c>BitmapFont</c>, which every <c>Text</c>
    /// sharing it sees mutate through a shared reference -- so growth here never frees or replaces
    /// resources a font it has already handed out still points at; the alternative would be a real
    /// use-after-free/stale-GPU-texture-id risk, since nothing else in the game can safely be told "the
    /// old struct copy you're holding is no longer valid."
    /// </summary>
    /// <remarks>
    /// <see cref="GlyphCapacity"/> reserves headroom in <see cref="CanonicalFont"/>'s Recs/Glyphs
    /// arrays, doubled whenever it's exceeded, so most growth calls write new entries into
    /// already-allocated space instead of allocating (and orphaning the previous) array every time --
    /// bounding the number of orphaned generations to O(log(final glyph count)) rather than one per
    /// growth call. The GPU texture gets the same treatment for free: <see cref="ApplyGrowth"/> only
    /// replaces it when KernSmith's own atlas actually had to resize
    /// (<see cref="GlyphAdditionResult.PageGrown"/>); otherwise it patches the new glyph pixels
    /// directly into the still-live texture via <c>UpdateTextureRec</c>, which is safe for the SAME
    /// reason writing into reserved array headroom is -- the identity (Texture.Id) never changes, so
    /// no struct copy anywhere is left pointing at something that got freed or replaced.
    /// </remarks>
    private sealed class GrowableFontState
    {
        public Raylib_cs.Font CanonicalFont;
        public int GlyphCapacity;
        public readonly int LineHeight;
        public readonly int BaselineY;

        public GrowableFontState(Raylib_cs.Font font, int lineHeight, int baselineY)
        {
            CanonicalFont = font;
            GlyphCapacity = font.GlyphCount;
            LineHeight = lineHeight;
            BaselineY = baselineY;
        }
    }

    // Keyed by BmfcSave.FontCacheFileName, the same identity TryCreateFont already names its textures
    // after -- mirrors KernSmith.MonoGameGum's KernSmithFontCreator._growableModels/_activeSessions.
    private readonly Dictionary<string, BmFontModel> _growableModels = new();
    private readonly Dictionary<string, GrowableFontState> _growableFonts = new();
    private readonly Dictionary<string, BmFontIncrementalSession> _activeSessions = new();

    // Atlas ceiling handed to KernSmith. KernSmith sizes each page to the smallest power-of-two
    // that fits, UP TO this max — so a generous ceiling collapses the whole glyph set onto one page
    // (sized to need, not to the ceiling) for any size this is used with, while staying within the
    // GL_MAX_TEXTURE_SIZE of effectively all GPUs. The default 512x256 is too small and spills to
    // multiple pages at larger sizes, which raylib's single-texture Font cannot represent.
    private const int SingleAtlasMaxSize = 4096;

    private readonly RasterizerBackend? _backend;

    /// <summary>
    /// Initializes a new instance of <see cref="KernSmithRaylibFontCreator"/>.
    /// </summary>
    /// <param name="backend">
    /// Optional rasterizer backend override. When null, uses the default (FreeType). Use
    /// <see cref="RasterizerBackend.StbTrueType"/> on platforms where native libraries are
    /// unavailable.
    /// </param>
    public KernSmithRaylibFontCreator(RasterizerBackend? backend = null)
    {
        _backend = backend;
    }

    /// <summary>
    /// Registers raw font data (TTF/OTF/WOFF) under a family name so that font generation can
    /// resolve it without accessing system fonts. The <c>byte[]</c> overload is the path for
    /// embedded theme fonts.
    /// </summary>
    /// <param name="familyName">Font family name (e.g., "Arial").</param>
    /// <param name="fontData">Raw font file bytes.</param>
    /// <param name="style">Optional style name (e.g., "Bold", "Italic"), or null for the default variant.</param>
    /// <param name="faceIndex">TTC face index (0 for single-face font files).</param>
    public static void RegisterFont(string familyName, byte[] fontData, string? style = null, int faceIndex = 0)
        => BmFont.RegisterFont(familyName, fontData, style, faceIndex);

    /// <summary>
    /// Registers a font file under a family name, reading it through
    /// <see cref="FileManager.GetStreamForFile"/> so a font that only exists behind a host stream
    /// hook (a .ttf packed into a .gumpkg, or one in a game's asset zip) registers the same as one
    /// on disk.
    /// </summary>
    /// <param name="familyName">Font family name (e.g., "Arial").</param>
    /// <param name="filePath">Path to a .ttf, .otf, or .woff font file.</param>
    /// <param name="style">Optional style name (e.g., "Bold", "Italic"), or null for the default variant.</param>
    /// <param name="faceIndex">TTC face index (0 for single-face font files).</param>
    public static void RegisterFont(string familyName, string filePath, string? style = null, int faceIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(familyName);
        ArgumentNullException.ThrowIfNull(filePath);

        BmFont.RegisterFont(familyName, GumFontGenerator.ReadFontFileBytes(filePath), style, faceIndex);
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
    public Raylib_cs.Font? TryCreateFont(BmfcSave bmfcSave)
    {
        // A generous atlas ceiling keeps the common case on a single page (KernSmith sizes the page
        // down to fit within it), so most fonts take the fast path below. Larger glyph sets that
        // still span multiple pages are merged into one texture afterward — raylib's Font is
        // single-texture, so it can't hold a page array the way the MonoGame BitmapFont does.
        bmfcSave.OutputWidth = SingleAtlasMaxSize;
        bmfcSave.OutputHeight = SingleAtlasMaxSize;
        BmFontResult result = GumFontGenerator.Generate(bmfcSave, _backend);

        if (result.Pages.Count == 0)
        {
            return null;
        }

        Texture2D primaryTexture;
        Raylib_cs.Font font;
        int[]? pageYOffsets = null;
        byte[] pixelData;
        int pageWidth = result.Pages[0].Width;
        int pageHeight = result.Pages[0].Height;
        int textureHeight;

        if (result.Pages.Count == 1)
        {
            pixelData = result.Pages[0].PixelData;
            textureHeight = pageHeight;
            primaryTexture = UploadTexture(pixelData, pageWidth, textureHeight);
            font = ContentLoader.BuildFontFromFntText(result.FntText, primaryTexture);
        }
        else
        {
            // Merge KernSmith's pages into a single texture. KernSmith sizes every page identically
            // (PackResult.PageWidth/Height), so stacking them vertically is a contiguous copy and each
            // glyph's atlas Y is shifted by its page's offset. Mirrors the MonoGame creator, which hands
            // BitmapFont a texture array — same behavior (a usable font for any glyph set), no fallback.
            int pageCount = result.Pages.Count;

            pixelData = new byte[pageWidth * pageHeight * pageCount * 4];
            pageYOffsets = new int[pageCount];
            for (int i = 0; i < pageCount; i++)
            {
                byte[] pagePixels = result.Pages[i].PixelData;
                System.Array.Copy(pagePixels, 0, pixelData, i * pagePixels.Length, pagePixels.Length);
                pageYOffsets[i] = i * pageHeight;
            }

            textureHeight = pageHeight * pageCount;
            primaryTexture = UploadTexture(pixelData, pageWidth, textureHeight);
            font = ContentLoader.BuildFontFromFntText(result.FntText, primaryTexture, pageYOffsets);
        }

        // Issue #4061: attach the shadow AtlasVariant (if requested) as a companion Font, registered
        // against the primary's atlas texture id (RaylibFontShadowRegistry — the Raylib counterpart of
        // MonoGame's BitmapFont.ShadowFont). The shadow gets its OWN texture upload (from the same
        // pixel data) rather than reusing primaryTexture: ManagedFont.Dispose calls Raylib.UnloadFont
        // on both fonts, which unloads each font's Texture — sharing one GPU texture between them
        // would double-free it.
        if (result.VariantModels.ContainsKey("shadow"))
        {
            string shadowFntText = result.GetVariantFntText("shadow");
            Texture2D shadowTexture = UploadTexture(pixelData, pageWidth, textureHeight);
            Raylib_cs.Font shadowFont = ContentLoader.BuildFontFromFntText(shadowFntText, shadowTexture, pageYOffsets);
            RaylibFontShadowRegistry.Register(font.Texture.Id, shadowFont);
        }

        // Issue #4546: remember this font's model so a later TryAddGlyphs call can resume an
        // incremental session against it. Mirrors KernSmith.MonoGameGum's KernSmithFontCreator, with
        // one extra restriction: only a font that came back as a SINGLE page is tracked as growable.
        // KernSmith's incremental sessions grow a single atlas (AdditionOverflowPolicy.Grow, never
        // NewPage -- see TextRuntime.MaxInMemoryFontAtlasSize's own doc comment on why growth
        // standardized on that policy), so there's no defined way to resume growth against a font that
        // was already multi-page at creation time -- a large enough initial Ranges character set to
        // exceed SingleAtlasMaxSize on its own, which is rare in practice. Dropshadow fonts are also
        // skipped: KernSmith's incremental sessions reject Variants outright.
        if (!string.IsNullOrEmpty(bmfcSave.FontFile) && !bmfcSave.HasDropshadow && result.Pages.Count == 1)
        {
            RaylibFontMetricsRegistry.TryGet(font.Texture.Id, out RaylibFontMetricsRegistry.FontLineMetrics metrics);
            _growableModels[bmfcSave.FontCacheFileName] = result.Model;
            _growableFonts[bmfcSave.FontCacheFileName] = new GrowableFontState(font, metrics.LineHeight, metrics.BaselineY);
        }

        return font;
    }

    /// <inheritdoc/>
    public IReadOnlyList<char>? TryAddGlyphs(ref Raylib_cs.Font font, BmfcSave bmfcSave, string characters)
    {
        string key = bmfcSave.FontCacheFileName;
        if (!_growableModels.TryGetValue(key, out BmFontModel? model)
            || !_growableFonts.TryGetValue(key, out GrowableFontState? state))
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
            ApplyGrowth(state, result);
        }

        font = state.CanonicalFont;
        return result.FailedCodepoints.Select(cp => (char)cp).ToArray();
    }

    /// <summary>
    /// Applies <paramref name="result"/>'s newly-added glyphs onto <paramref name="state"/>'s canonical
    /// font, amortizing the cost per <see cref="GrowableFontState"/>'s own doc comment: the texture is
    /// only rebuilt when the atlas itself had to resize (<see cref="GlyphAdditionResult.PageGrown"/>),
    /// and the Recs/Glyphs arrays are only reallocated (geometrically) when
    /// <see cref="GrowableFontState.GlyphCapacity"/> is exhausted.
    /// </summary>
    private static unsafe void ApplyGrowth(GrowableFontState state, GlyphAdditionResult result)
    {
        Texture2D texture = state.CanonicalFont.Texture;
        if (result.PageGrown)
        {
            // The live atlas no longer fits its own previous size -- rebuild it as a full CPU-side
            // composite (read back the current atlas, blit the new glyphs into it, reupload) into a
            // brand-new texture. The superseded Texture is deliberately never unloaded here; see
            // GrowableFontState's own doc comment.
            Image oldImage = Raylib.LoadImageFromTexture(texture);
            byte[] buffer = new byte[result.PageWidth * result.PageHeight * 4];
            CopyImageInto(oldImage, buffer, result.PageWidth);
            Raylib.UnloadImage(oldImage);

            foreach (AddedGlyph glyph in result.Added)
            {
                WriteGlyphPixels(buffer, result.PageWidth, glyph);
            }

            texture = UploadTexture(buffer, result.PageWidth, result.PageHeight);
            ContentLoader.TextureFilterApplier(texture, ContentLoader.DefaultTextureFilter);
            RaylibFontMetricsRegistry.Register(texture.Id, state.LineHeight, state.BaselineY);
        }
        else
        {
            // The new glyphs fit in the atlas's current size -- patch their pixels directly into the
            // STILL-LIVE texture instead of rebuilding it. Safe for the same reason writing into
            // reserved Recs/Glyphs headroom below is: the texture's identity (Texture.Id) never
            // changes, so every struct copy of this font -- old or new -- keeps sampling the same GPU
            // resource, just with more of it filled in once its own copy's GlyphCount catches up.
            foreach (AddedGlyph glyph in result.Added)
            {
                UpdateTextureRegion(texture, glyph);
            }
        }

        int oldGlyphCount = state.CanonicalFont.GlyphCount;
        int newGlyphCount = oldGlyphCount + result.Added.Count;

        Rectangle* recs;
        GlyphInfo* glyphs;
        if (newGlyphCount <= state.GlyphCapacity)
        {
            // Room already reserved from an earlier growth -- write the new entries into the existing
            // arrays' unused tail. Older struct copies of this font have their own (smaller) GlyphCount
            // and never index past it, so they're unaffected by entries appended beyond it.
            recs = state.CanonicalFont.Recs;
            glyphs = state.CanonicalFont.Glyphs;
        }
        else
        {
            int newCapacity = System.Math.Max(newGlyphCount, state.GlyphCapacity * 2);
            recs = (Rectangle*)Raylib.MemAlloc((uint)(newCapacity * sizeof(Rectangle)));
            glyphs = (GlyphInfo*)Raylib.MemAlloc((uint)(newCapacity * sizeof(GlyphInfo)));

            Buffer.MemoryCopy(state.CanonicalFont.Recs, recs,
                (long)newCapacity * sizeof(Rectangle), (long)oldGlyphCount * sizeof(Rectangle));
            Buffer.MemoryCopy(state.CanonicalFont.Glyphs, glyphs,
                (long)newCapacity * sizeof(GlyphInfo), (long)oldGlyphCount * sizeof(GlyphInfo));

            state.GlyphCapacity = newCapacity;
        }

        int index = oldGlyphCount;
        foreach (AddedGlyph glyph in result.Added)
        {
            recs[index] = new Rectangle(glyph.X, glyph.Y, glyph.Width, glyph.Height);
            glyphs[index] = new GlyphInfo
            {
                Value = glyph.Char.Id,
                OffsetX = glyph.Char.XOffset,
                OffsetY = glyph.Char.YOffset,
                AdvanceX = glyph.Char.XAdvance,
            };
            index++;
        }

        state.CanonicalFont = new Raylib_cs.Font
        {
            BaseSize = state.CanonicalFont.BaseSize,
            GlyphCount = newGlyphCount,
            GlyphPadding = 0,
            Texture = texture,
            Recs = recs,
            Glyphs = glyphs,
        };
    }

    /// <summary>
    /// Blits one newly-added glyph's tightly-packed RGBA32 pixels directly into the live GPU texture at
    /// its placed (X, Y) position, without reallocating or otherwise touching the rest of the texture.
    /// </summary>
    private static unsafe void UpdateTextureRegion(Texture2D texture, AddedGlyph glyph)
    {
        fixed (byte* p = glyph.Pixels)
        {
            Raylib.UpdateTextureRec(texture, new Rectangle(glyph.X, glyph.Y, glyph.Width, glyph.Height), p);
        }
    }

    /// <summary>
    /// Copies <paramref name="source"/>'s full pixel content into <paramref name="destination"/> at
    /// (0, 0), respecting the row stride when <paramref name="destinationWidth"/> differs from the
    /// source's own width (the atlas grew wider). Assumes both are tightly-packed RGBA32.
    /// </summary>
    private static unsafe void CopyImageInto(Image source, byte[] destination, int destinationWidth)
    {
        byte* src = (byte*)source.Data;
        int rowBytes = source.Width * 4;
        for (int y = 0; y < source.Height; y++)
        {
            int destinationOffset = y * destinationWidth * 4;
            System.Runtime.InteropServices.Marshal.Copy((System.IntPtr)(src + (long)y * rowBytes), destination, destinationOffset, rowBytes);
        }
    }

    /// <summary>
    /// Blits one newly-added glyph's tightly-packed RGBA32 pixels into <paramref name="buffer"/> at its
    /// placed (X, Y) position.
    /// </summary>
    private static void WriteGlyphPixels(byte[] buffer, int bufferWidth, AddedGlyph glyph)
    {
        int rowBytes = glyph.Width * 4;
        for (int row = 0; row < glyph.Height; row++)
        {
            int sourceOffset = row * rowBytes;
            int destinationOffset = ((glyph.Y + row) * bufferWidth + glyph.X) * 4;
            System.Buffer.BlockCopy(glyph.Pixels, sourceOffset, buffer, destinationOffset, rowBytes);
        }
    }

    private static unsafe Texture2D UploadTexture(byte[] pixels, int width, int height)
    {
        Texture2D texture;
        fixed (byte* p = pixels)
        {
            Image image = new Image
            {
                Data = p,
                Width = width,
                Height = height,
                Mipmaps = 1,
                Format = Raylib_cs.PixelFormat.UncompressedR8G8B8A8,
            };
            // LoadTextureFromImage copies the pixels to the GPU, so the pinned pointer only needs
            // to stay valid for the duration of this call.
            texture = Raylib.LoadTextureFromImage(image);
        }
        return texture;
    }
}
