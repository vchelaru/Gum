#nullable enable
using Gum.DataTypes;
using Gum.Wireframe;

#if SKIA
using SkiaSharp;
using Svg.Skia;
using ContainedRenderable = SkiaGum.VectorSprite;
using SvgDocument = Svg.Skia.SKSvg;
#else
using Gum.Content;
using ContainedRenderable = Gum.Renderables.Svg;
using SvgDocument = Apos.Shapes.ShapeSvg;
#endif

#if FRB
#if SKIA
namespace SkiaGum.GueDeriving;
#else
namespace MonoGameGum.GueDeriving;
#endif
#else
namespace Gum.GueDeriving;
#endif

/// <summary>
/// Runtime that draws an SVG document. One source file for both backends: SkiaGum draws through a
/// <c>VectorSprite</c> wrapping an <c>SKSvg</c>, and the XNA family (MonoGameGumShapes /
/// KniGumShapes) draws through Apos.Shapes. SkiaGum is the canonical home, linked into the
/// Apos csprojs from <c>Runtimes/GumShapes/Directory.Build.props</c>.
/// </summary>
/// <remarks>
/// <see cref="Texture"/> is an <c>SKSvg</c> on Skia and an Apos.Shapes <c>ShapeSvg</c> on the XNA
/// family, the same way <c>SpriteRuntime.Texture</c> differs by backend.
///
/// The color API (<c>Color</c>, <c>Red</c>, <c>Green</c>, <c>Blue</c>, <c>Alpha</c>) is Skia only.
/// Skia modulates the drawing through an <c>SKColorFilter</c> color matrix. Apos's colored
/// <c>DrawSvg</c> overload instead <i>replaces</i> every paint in the file, producing a
/// silhouette. Exposing the same property names for a different result is worse than omitting
/// them, so the Apos side draws the file's own colors. A silhouette override can be added later
/// under a name that says so. Issue #4506.
/// </remarks>
public class SvgRuntime : InteractiveGue
{
    ContainedRenderable? mContainedSvg;
    ContainedRenderable ContainedSvg
    {
        get
        {
            mContainedSvg ??= this.RenderableComponent as ContainedRenderable
                ?? throw new System.InvalidOperationException(
                    $"{nameof(SvgRuntime)} has no {typeof(ContainedRenderable).Name} renderable. Construct it with fullInstantiation: true or call SetContainedObject first.");
            return mContainedSvg;
        }
    }

    string? sourceFile;

    /// <summary>
    /// Path to the .svg file to draw. Assigning re-resolves the document, which is cached by
    /// standardized path so the same file assigned to many runtimes parses once. A missing or
    /// unreadable file clears <see cref="Texture"/> and draws nothing rather than throwing.
    /// </summary>
    public string? SourceFile
    {
        // eventually we may want to store this off somehow
        get => sourceFile;
        set
        {
            if (sourceFile != value)
            {
                sourceFile = value;
#if SKIA
                if (string.IsNullOrEmpty(value))
                {
                    Texture = null;
                }
                else
                {
                    var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                    SvgDocument? document = loaderManager.LoadContent<SvgDocument>(value);
                    Texture = document;
                }
#else
                Texture = ShapeSvgLoader.Load(value!);
#endif
            }
        }
    }

    /// <summary>
    /// The loaded document, or <c>null</c> when nothing is loaded. Settable directly for callers
    /// that load or generate a document themselves instead of going through
    /// <see cref="SourceFile"/>.
    /// </summary>
    public SvgDocument? Texture
    {
        get => ContainedSvg.Texture;
        set => ChangeRenderableAndUpdateLayout((renderable: ContainedSvg, value), static state => state.renderable.Texture = state.value);
    }

#if SKIA
    public int Alpha
    {
        get => ContainedSvg.Alpha;
        set => ContainedSvg.Alpha = value;
    }

    public int Red
    {
        get => ContainedSvg.Red;
        set => ContainedSvg.Red = value;
    }

    public int Green
    {
        get => ContainedSvg.Green;
        set => ContainedSvg.Green = value;
    }

    public int Blue
    {
        get => ContainedSvg.Blue;
        set => ContainedSvg.Blue = value;
    }

    public SKColor Color
    {
        get => ContainedSvg.Color;
        set => ContainedSvg.Color = value;
    }
#endif

    /// <summary>
    /// Initializes a new SvgRuntime. When <paramref name="fullInstantiation"/> is true (the
    /// default), the underlying renderable is created and default values applied (Width = 100 with
    /// height following the file's aspect ratio). Pass false only when the runtime is being
    /// constructed by deserialization, which sets up the renderable separately.
    /// </summary>
    public SvgRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new ContainedRenderable());

            // Give it some good defaults:
            WidthUnits = DimensionUnitType.Absolute;
            HeightUnits = DimensionUnitType.MaintainFileAspectRatio;

            Width = 100;
            Height = 100;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (SvgRuntime)base.Clone();

        toReturn.mContainedSvg = null;

        return toReturn;
    }
}
