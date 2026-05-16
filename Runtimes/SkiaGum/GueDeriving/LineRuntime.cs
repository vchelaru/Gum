using Gum.DataTypes;
using Gum.Wireframe;

#if SKIA
using SkiaGum.Renderables;
using SkiaSharp;
#else
using MonoGameAndGum.Renderables;
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
/// Runtime for a line shape that draws from the top-left to the bottom-right of its bounding
/// rectangle. Rotation, Width, and Height can be used to control the line's angle and length.
/// </summary>
/// <remarks>
/// Source-shared between SkiaGum and MonoGameGumShapes (and KniGumShapes) via a Compile/Link in
/// the Apos-side csprojs - see <c>RoundedRectangleRuntime</c> for the same pattern. Platform
/// differences are gated behind <c>#if SKIA</c>; the cross-platform shape (constructor defaults,
/// IsRounded property, base wiring) is shared.
/// </remarks>
public class LineRuntime
#if SKIA
    : SkiaShapeRuntime
#else
    : AposShapeRuntime
#endif
{
    #region Contained Renderable
    protected override RenderableShapeBase ContainedRenderable => ContainedLine;

    Line? _containedLine;
    Line ContainedLine
    {
        get
        {
            if (_containedLine == null)
            {
                _containedLine = (Line)this.RenderableComponent;
            }
            return _containedLine;
        }
    }
    #endregion

    /// <summary>
    /// Whether the line endpoints are rounded. If false, endpoints are flat (butt cap).
    /// </summary>
    public bool IsRounded
    {
        get => ContainedLine.IsRounded;
        set => ContainedLine.IsRounded = value;
    }

    public LineRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
#if SKIA
            SetContainedObject(new Line());
#pragma warning disable CS0618 // Color is obsolete; migration to FillColor/StrokeColor tracked in #2790 (depends on two-slot composition).
            this.Color = SKColors.White;
#pragma warning restore CS0618
#else
            SetContainedShape(new Line());
            this.Color = Microsoft.Xna.Framework.Color.White;
#endif

            Width = 100;
            Height = 0;
            StrokeWidth = 2;
            StrokeWidthUnits = DimensionUnitType.ScreenPixel;

            DropshadowAlpha = 255;
            DropshadowRed = 0;
            DropshadowGreen = 0;
            DropshadowBlue = 0;

            DropshadowOffsetX = 0;
            DropshadowOffsetY = 3;
            DropshadowBlurX = 0;
            DropshadowBlurY = 3;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (LineRuntime)base.Clone();

        // Reset the cached renderable reference so the clone re-resolves it from its own
        // RenderableComponent rather than holding a reference to the source instance's renderable.
        // Skia previously did this; Apos previously did not (latent bug, fixed by unification).
        toReturn._containedLine = null;

        return toReturn;
    }
}
