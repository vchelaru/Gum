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
    public unsafe Raylib_cs.Font? TryCreateFont(BmfcSave bmfcSave)
    {
        // Force the whole glyph set onto a single page (raylib's Font is single-texture). The
        // default 512x256 atlas spills to multiple pages at larger sizes — later-page glyphs would
        // sample the wrong region and render as garbage — so request a generous atlas ceiling and
        // let KernSmith size the actual page down to fit.
        bmfcSave.OutputWidth = SingleAtlasMaxSize;
        bmfcSave.OutputHeight = SingleAtlasMaxSize;
        BmFontResult result = GumFontGenerator.Generate(bmfcSave, _backend);

        // Expected to be a single page for any practical size; guard defensively (empty glyph set,
        // or a glyph set too large for the ceiling) and fall back to the existing system-font path
        // rather than render a partial atlas.
        if (result.Pages.Count != 1)
        {
            return null;
        }

        AtlasPage page = result.Pages[0];

        Texture2D texture;
        fixed (byte* pixels = page.PixelData)
        {
            Image image = new Image
            {
                Data = pixels,
                Width = page.Width,
                Height = page.Height,
                Mipmaps = 1,
                Format = Raylib_cs.PixelFormat.UncompressedR8G8B8A8,
            };
            // LoadTextureFromImage copies the pixels to the GPU, so the pinned pointer only needs
            // to stay valid for the duration of this call.
            texture = Raylib.LoadTextureFromImage(image);
        }

        return ContentLoader.BuildFontFromFntText(result.FntText, texture);
    }
}
