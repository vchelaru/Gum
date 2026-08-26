using Apos.Shapes;
using Gum.DataTypes;
using Gum.Wireframe;
using MonoGameAndGum.Content;
using MonoGameAndGum.Renderables;

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

/// <summary>
/// Runtime that draws an SVG document, backed by Apos.Shapes. The XNA-family counterpart to
/// SkiaGum's <c>Gum.GueDeriving.SvgRuntime</c> (which wraps a Skia <c>VectorSprite</c>).
/// </summary>
/// <remarks>
/// This is deliberately a separate file from SkiaGum's <c>SvgRuntime</c> rather than the shared
/// source + <c>#if SKIA</c> arrangement the other Apos-and-Skia runtime pairs use
/// (<c>RoundedRectangleRuntime</c>, <c>ArcRuntime</c>, <c>LineRuntime</c>): the two sides share
/// almost no implementation — <c>SKSvg</c> versus <see cref="ShapeSvg"/>, <c>VectorSprite</c>
/// versus <see cref="Svg"/>, <c>SKColor</c> versus XNA <c>Color</c> — so a shared file would be
/// gated on nearly every member. Both types carry the same fully-qualified name in different
/// assemblies, which never meet in one build. Issue #4506.
///
/// Two deliberate differences from the Skia runtime:
/// <list type="bullet">
/// <item><description>
/// <b>No color API.</b> Skia's <c>Color</c>/<c>Red</c>/<c>Green</c>/<c>Blue</c>/<c>Alpha</c>
/// modulate the drawing through an <c>SKColorFilter</c> color matrix. Apos's colored
/// <c>DrawSvg</c> overload instead <i>replaces</i> every paint in the file, producing a
/// silhouette. Exposing the same property names for a different result is worse than omitting
/// them, so this draws the file's own colors. A silhouette override can be added later under a
/// name that says so.
/// </description></item>
/// <item><description>
/// <b>Aspect-locked sizing.</b> See <see cref="Svg.Render"/> — height drives a uniform scale, so
/// a non-uniform Width is aspect-corrected rather than stretched.
/// </description></item>
/// </list>
/// </remarks>
public class SvgRuntime : InteractiveGue
{
    Svg? mContainedSvg;
    Svg ContainedSvg
    {
        get
        {
            mContainedSvg ??= (Svg)this.RenderableComponent;
            return mContainedSvg;
        }
    }

    string? sourceFile;

    /// <summary>
    /// Path to the .svg file to draw. Assigning re-resolves the document through
    /// <see cref="ShapeSvgLoader"/>, which caches by standardized path — so the same file assigned
    /// to many runtimes parses once. A missing or unreadable file clears the document and draws
    /// nothing rather than throwing.
    /// </summary>
    public string? SourceFile
    {
        get => sourceFile;
        set
        {
            if (sourceFile != value)
            {
                sourceFile = value;
                Document = ShapeSvgLoader.Load(value!);
            }
        }
    }

    /// <summary>
    /// The loaded document, or <c>null</c> when nothing is loaded. Settable directly for callers
    /// that load or generate a <see cref="ShapeSvg"/> themselves instead of going through
    /// <see cref="SourceFile"/>.
    /// </summary>
    public ShapeSvg? Document
    {
        get => ContainedSvg.Document;
        set => ContainedSvg.Document = value;
    }

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
            SetContainedObject(new Svg());

            // Same defaults as SkiaGum's SvgRuntime so a screen built against one backend sizes
            // identically on the other.
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
