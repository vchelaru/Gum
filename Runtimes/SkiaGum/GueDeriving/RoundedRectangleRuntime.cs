using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

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
/// Runtime that draws a rectangle with rounded corners, sized by its Width and Height.
/// Use <see cref="CornerRadius"/> to control how rounded the corners are; a value of 0 produces a
/// sharp-cornered rectangle.
/// </summary>
/// <remarks>
/// Source-shared between SkiaGum and MonoGameGumShapes (and KniGumShapes) via a Compile/Link in
/// the Apos-side csprojs - see CustomSetPropertyOnRenderable.cs for the same pattern. Platform
/// differences are gated behind <c>#if SKIA</c>; the cross-platform shape (constructor defaults,
/// CornerRadius property, base wiring) is shared.
/// </remarks>
public class RoundedRectangleRuntime
#if SKIA
    : SkiaShapeRuntime, IClipPath
#else
    : AposShapeRuntime
#endif
{
    #region Contained Renderable
    protected override RenderableShapeBase ContainedRenderable => ContainedRoundedRectangle;

    RoundedRectangle? mContainedRoundedRectangle;
    RoundedRectangle ContainedRoundedRectangle
    {
        get
        {
            if (mContainedRoundedRectangle == null)
            {
                mContainedRoundedRectangle = (RoundedRectangle)this.RenderableComponent;
            }
            return mContainedRoundedRectangle;
        }
    }

    #endregion

    /// <summary>
    /// Gets or sets the radius, in pixels, of each rounded corner. A value of 0 produces a
    /// sharp-cornered rectangle.
    /// </summary>
    public float CornerRadius
    {
#if SKIA
        get;
        set;
#else
        get => ContainedRoundedRectangle.CornerRadius;
        set => ContainedRoundedRectangle.CornerRadius = value;
#endif
    }

#if SKIA
    // Skia-only: per-corner overrides and unit-aware corner radius. Apos.Shapes' RoundedRectangle
    // renderable doesn't currently expose per-corner radii or ScreenPixel scaling. Tracked as a
    // future parity item; gated here so unification doesn't block on the deeper Apos work.

    public float? CustomRadiusTopLeft { get; set; } = null;
    public float? CustomRadiusTopRight { get; set; } = null;
    public float? CustomRadiusBottomRight { get; set; } = null;
    public float? CustomRadiusBottomLeft { get; set; } = null;

    public DimensionUnitType CornerRadiusUnits
    {
        get; set;
    }

    public SKPath GetClipPath() => ContainedRoundedRectangle.GetClipPath();
#endif

    /// <summary>
    /// Initializes a new RoundedRectangleRuntime. When <paramref name="fullInstantiation"/> is
    /// true (the default), an underlying RoundedRectangle renderable is created and default values
    /// are applied (Width = Height = 100, CornerRadius = 5). Pass false only when the runtime is
    /// being constructed by deserialization, which sets up the renderable separately.
    /// </summary>
    public RoundedRectangleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedShape(new RoundedRectangle());

            // Defaults of 100 to match Glue / FRB conventions.
            Width = 100;
            Height = 100;

            CornerRadius = 5;

            DropshadowAlpha = 255;
            DropshadowRed = 0;
            DropshadowGreen = 0;
            DropshadowBlue = 0;

            DropshadowOffsetX = 0;
            DropshadowOffsetY = 3;
            DropshadowBlurX = 0;
            DropshadowBlurY = 3;

            GradientType = GradientType.Linear;

            GradientX1 = 0;
            GradientX1Units = GeneralUnitType.PixelsFromSmall;

            GradientY1 = 0;
            GradientY1Units = GeneralUnitType.PixelsFromSmall;

            Red1 = 255;
            Green1 = 255;
            Blue1 = 255;

            GradientX2 = 100;
            GradientX2Units = GeneralUnitType.PixelsFromSmall;

            GradientY2 = 100;
            GradientY2Units = GeneralUnitType.PixelsFromSmall;

            Red2 = 255;
            Green2 = 255;
            Blue2 = 0;

#if !SKIA
            // Apos: explicitly set the solid Color to white so the rectangle renders white by
            // default when IsFilled is true. Skia intentionally does NOT set this - the Skia path
            // is also used by MAUI, where the renderable's default color is the right one to keep
            // (overriding to white here breaks MAUI rendering in some host configurations). This
            // divergence is the only intentional default-color difference between the two paths.
            Red = 255;
            Green = 255;
            Blue = 255;
#endif
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (RoundedRectangleRuntime)base.Clone();

        // Reset the cached renderable reference so the clone re-resolves it from its own
        // RenderableComponent rather than holding a reference to the source instance's renderable.
        // Without this, mutating the clone's CornerRadius (or any property routed through the
        // cached field) would update the original. Skia previously did this; Apos previously did
        // not (latent bug, fixed by unification).
        toReturn.mContainedRoundedRectangle = null;

        return toReturn;
    }

#if SKIA
    public override void PreRender()
    {
        if (this.EffectiveManagers != null)
        {
            var camera = this.EffectiveManagers.Renderer.Camera;
            var cornerRadius = CornerRadius;
            var topLeft = CustomRadiusTopLeft;
            var topRight = CustomRadiusTopRight;
            var bottomLeft = CustomRadiusBottomLeft;
            var bottomRight = CustomRadiusBottomRight;

            switch (CornerRadiusUnits)
            {
                case DimensionUnitType.Absolute:
                    // do nothing
                    break;
                case DimensionUnitType.ScreenPixel:
                    cornerRadius /= camera.Zoom;

                    topLeft /= camera.Zoom;
                    topRight /= camera.Zoom;
                    bottomLeft /= camera.Zoom;
                    bottomRight /= camera.Zoom;

                    break;
            }
            ContainedRoundedRectangle.CornerRadius = cornerRadius;
            ContainedRoundedRectangle.CustomRadiusTopLeft = topLeft;
            ContainedRoundedRectangle.CustomRadiusTopRight = topRight;
            ContainedRoundedRectangle.CustomRadiusBottomLeft = bottomLeft;
            ContainedRoundedRectangle.CustomRadiusBottomRight = bottomRight;
        }
        base.PreRender();
    }
#endif
}
