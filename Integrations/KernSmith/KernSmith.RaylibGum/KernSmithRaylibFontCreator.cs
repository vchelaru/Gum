using KernSmith.Atlas;
using KernSmith.Output;
using KernSmith.Rasterizer;
using Raylib_cs;
using RaylibGum.Renderables;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics.Fonts;

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
public class KernSmithRaylibFontCreator : IRaylibFontCreator
{
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

        if (result.Pages.Count == 1)
        {
            AtlasPage page = result.Pages[0];
            Texture2D texture = UploadTexture(page.PixelData, page.Width, page.Height);
            return ContentLoader.BuildFontFromFntText(result.FntText, texture);
        }

        // Merge KernSmith's pages into a single texture. KernSmith sizes every page identically
        // (PackResult.PageWidth/Height), so stacking them vertically is a contiguous copy and each
        // glyph's atlas Y is shifted by its page's offset. Mirrors the MonoGame creator, which hands
        // BitmapFont a texture array — same behavior (a usable font for any glyph set), no fallback.
        int pageWidth = result.Pages[0].Width;
        int pageHeight = result.Pages[0].Height;
        int pageCount = result.Pages.Count;

        byte[] merged = new byte[pageWidth * pageHeight * pageCount * 4];
        int[] pageYOffsets = new int[pageCount];
        for (int i = 0; i < pageCount; i++)
        {
            byte[] pagePixels = result.Pages[i].PixelData;
            System.Array.Copy(pagePixels, 0, merged, i * pagePixels.Length, pagePixels.Length);
            pageYOffsets[i] = i * pageHeight;
        }

        Texture2D mergedTexture = UploadTexture(merged, pageWidth, pageHeight * pageCount);
        return ContentLoader.BuildFontFromFntText(result.FntText, mergedTexture, pageYOffsets);
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
