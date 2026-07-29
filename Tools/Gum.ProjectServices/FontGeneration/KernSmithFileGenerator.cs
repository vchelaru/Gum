using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using KernSmith;
using KernSmith.Output;
using KernSmith.Rasterizer;
using KernSmith.Rasterizers.FreeType;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace Gum.ProjectServices.FontGeneration;

/// <summary>
/// Generates bitmap font files using the KernSmith library.
/// Cross-platform: does not require Windows or any external executable.
/// </summary>
public class KernSmithFileGenerator : IFontFileGenerator
{
    private static bool _rasterizerRegistered;
    private readonly IFontGenerationCallbacks _callbacks;

    /// <summary>
    /// Initializes a new instance of <see cref="KernSmithFileGenerator"/>.
    /// </summary>
    /// <param name="callbacks">
    /// Optional callbacks for output logging. When <c>null</c>, all feedback is suppressed.
    /// </param>
    public KernSmithFileGenerator(IFontGenerationCallbacks? callbacks = null)
    {
        _callbacks = callbacks ?? new NoOpFontGenerationCallbacks();
        EnsureRasterizerRegistered();
    }

    /// <summary>
    /// KernSmith sizes its own atlas via <see cref="FontGeneratorOptions.AutofitTexture"/>, so the
    /// caller's <c>OutputWidth</c>/<c>OutputHeight</c> heuristic (built for bmfont.exe, which can't
    /// autofit) is unnecessary here.
    /// </summary>
    public bool RequiresSizeEstimation => false;

    private static void EnsureRasterizerRegistered()
    {
        if (_rasterizerRegistered)
        {
            return;
        }
        if (!RasterizerFactory.IsRegistered(RasterizerBackend.FreeType))
        {
            RasterizerFactory.Register(RasterizerBackend.FreeType, () => new FreeTypeRasterizer());
        }
        _rasterizerRegistered = true;
    }

    /// <inheritdoc/>
    public async Task<GeneralResponse> GenerateFont(BmfcSave bmfcSave, string outputFntPath, bool createTask)
    {
        if (createTask)
        {
            return await Task.Run(() => GenerateFontCore(bmfcSave, outputFntPath));
        }

        return GenerateFontCore(bmfcSave, outputFntPath);
    }

    private GeneralResponse GenerateFontCore(BmfcSave bmfcSave, string outputFntPath)
    {
        GeneralResponse response = new GeneralResponse();

        try
        {
            FontGeneratorOptions options = BuildOptions(bmfcSave);

            var stopwatch = Stopwatch.StartNew();

            BmFontResult result = string.IsNullOrEmpty(bmfcSave.FontFile)
                ? BmFont.GenerateFromSystem(bmfcSave.FontName, options)
                : BmFont.Generate(bmfcSave.FontFile, options);

            // ToFile expects a base path WITHOUT the .fnt extension.
            string basePath = Path.ChangeExtension(outputFntPath, null);
            result.ToFile(basePath);

            stopwatch.Stop();

            if (File.Exists(outputFntPath))
            {
                response.Succeeded = true;
                response.Message = string.Empty;
                _callbacks.OnOutput($"KernSmith ({stopwatch.ElapsedMilliseconds}ms) : generated \"{bmfcSave.FontName}\" size {bmfcSave.FontSize} -> {outputFntPath} ");
            }
            else
            {
                response.Succeeded = false;
                response.Message = "KernSmith completed but the expected .fnt file was not found on disk.";
            }
        }
        catch (Exception ex)
        {
            response.Succeeded = false;
            response.Message = $"KernSmith font generation failed: {ex.Message}";
            _callbacks.OnOutput($"KernSmith error: {ex.Message}");
        }

        return response;
    }

    /// <summary>
    /// Largest atlas dimension KernSmith is allowed to grow to via <see cref="FontGeneratorOptions.AutofitTexture"/>,
    /// matching the largest size in <see cref="HeadlessFontGenerationService"/>'s bmfont.exe binary-search table.
    /// </summary>
    private const int MaxAtlasDimension = 8192;

    internal static FontGeneratorOptions BuildOptions(BmfcSave bmfcSave)
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
        // KernSmith picks the smallest atlas that fits everything on one page (capped by
        // MaxTextureWidth/Height below) instead of relying on Gum's own bmfont.exe-oriented size
        // guess, which doesn't account for dropshadow inflation and can undersize the atlas.
        options.AutofitTexture = true;
        options.MaxTextureWidth = MaxAtlasDimension;
        options.MaxTextureHeight = MaxAtlasDimension;

        // Drop shadow renders as a two-pass draw at runtime (issue #4001): a ShadowSilhouette atlas
        // variant, packed into the same shared PNG as the primary, is drawn offset + tinted under
        // each glyph. Nothing about the shadow is baked into the primary anymore -- baking is what
        // caused the runtime text-color modulate to re-tint the shadow. KernSmith forbids a custom
        // ChannelConfig alongside Variants, so the primary keeps the default glyph-coverage layout
        // when dropshadow is on.
        if (!bmfcSave.HasDropshadow)
        {
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

        List<int> codepoints = BmfcSave.ParseCharRanges(bmfcSave.Ranges);
        options.Characters = CharacterSet.FromChars(codepoints);

        if (bmfcSave.HasDropshadow)
        {
            // Offset and color are applied by the runtime at draw time; the silhouette bakes only
            // its blurred coverage shape, so BlurRadius is the sole shadow parameter that affects
            // the atlas.
            options.Variants = new[]
            {
                new AtlasVariant("shadow", AtlasVariantKind.ShadowSilhouette,
                    BlurRadius: (int)MathF.Round(bmfcSave.DropshadowBlur)),
            };
        }

        return options;
    }

    /// <summary>
    /// Default no-op implementation used when no callbacks are supplied.
    /// </summary>
    private sealed class NoOpFontGenerationCallbacks : IFontGenerationCallbacks { }
}
