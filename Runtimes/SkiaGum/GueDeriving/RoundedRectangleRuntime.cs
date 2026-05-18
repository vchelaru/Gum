using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;

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
[Obsolete("Use RectangleRuntime with CornerRadius (plus FillColor / StrokeColor for the two-slot model) instead. RoundedRectangleRuntime will be removed in a future release. See docs/gum-tool/upgrading/migrating-to-2026-may.md for the full migration guide.")]
public class RoundedRectangleRuntime
#if SKIA
    : SkiaShapeRuntime, IClipPath
#else
    : AposShapeRuntime
#endif
{
    #region Contained Renderable
    protected override RenderableShapeBase ContainedRenderable => ContainedRoundedRectangle;

    RoundedRectangle? _containedRoundedRectangle;
    RoundedRectangle ContainedRoundedRectangle
    {
        get
        {
            if (_containedRoundedRectangle == null)
            {
                _containedRoundedRectangle = (RoundedRectangle)this.RenderableComponent;
            }
            return _containedRoundedRectangle;
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

    // Per-corner overrides. Available on both backends as of Apos.Shapes 0.6.9 (PR #32, which
    // added the CornerRadii overload to DrawRectangle). The Skia side stores them as auto-props
    // and copies them to the renderable in PreRender (so the ScreenPixel scaling below applies).
    // The Apos side forwards directly to the renderable, the same way CornerRadius does — no
    // ScreenPixel scaling on Apos yet (still tracked as a parity item).
    public float? CustomRadiusTopLeft
    {
#if SKIA
        get;
        set;
#else
        get => ContainedRoundedRectangle.CustomRadiusTopLeft;
        set => ContainedRoundedRectangle.CustomRadiusTopLeft = value;
#endif
    }
    public float? CustomRadiusTopRight
    {
#if SKIA
        get;
        set;
#else
        get => ContainedRoundedRectangle.CustomRadiusTopRight;
        set => ContainedRoundedRectangle.CustomRadiusTopRight = value;
#endif
    }
    public float? CustomRadiusBottomRight
    {
#if SKIA
        get;
        set;
#else
        get => ContainedRoundedRectangle.CustomRadiusBottomRight;
        set => ContainedRoundedRectangle.CustomRadiusBottomRight = value;
#endif
    }
    public float? CustomRadiusBottomLeft
    {
#if SKIA
        get;
        set;
#else
        get => ContainedRoundedRectangle.CustomRadiusBottomLeft;
        set => ContainedRoundedRectangle.CustomRadiusBottomLeft = value;
#endif
    }

#if SKIA
    // Skia-only: unit-aware corner radius. Apos.Shapes' RoundedRectangle renderable doesn't
    // currently honor ScreenPixel scaling. Tracked as a future parity item.

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

#if SKIA
            // Issue #2814: opt this Skia runtime into two-slot fill+stroke composition (recipe
            // documented on SkiaShapeRuntime). The contained RoundedRectangle is the fill slot;
            // a second RoundedRectangle, parented under fill, draws the stroke. Defaults below
            // (no explicit FillColor / StrokeColor) keep pre-#2814 visual behavior - neither
            // slot lights up until the user sets a color - but the slots are now independent.
            SetStrokeRenderable(new RoundedRectangle());
#endif

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
        toReturn._containedRoundedRectangle = null;

#if SKIA
        // Issue #2814 recipe (mirror of CircleRuntime.Clone): drop the inherited reference to
        // the source stroke slot and rebuild a fresh one parented to the clone fill so the
        // clone is fully independent.
        toReturn.ClearStrokeRenderable();
        toReturn.SetStrokeRenderable(new RoundedRectangle());
        // Re-fire StrokeColor so the user color (held on _strokeColor via MemberwiseClone) is
        // pushed into the fresh stroke slot.
        toReturn.StrokeColor = toReturn.StrokeColor;
#endif

        return toReturn;
    }

#if SKIA
    public override void PreRender()
    {
        // Resolve unit-aware corner radii once. ScreenPixel scaling needs a Camera (lives on
        // EffectiveManagers) but the un-scaled Absolute path also wants the values pushed onto
        // the contained renderable + (since #2814) the stroke slot, so the EffectiveManagers
        // gate is narrow: only the divide-by-zoom step requires it.
        var cornerRadius = CornerRadius;
        var topLeft = CustomRadiusTopLeft;
        var topRight = CustomRadiusTopRight;
        var bottomLeft = CustomRadiusBottomLeft;
        var bottomRight = CustomRadiusBottomRight;

        if (CornerRadiusUnits == DimensionUnitType.ScreenPixel && this.EffectiveManagers != null)
        {
            var camera = this.EffectiveManagers.Renderer.Camera;
            cornerRadius /= camera.Zoom;

            topLeft /= camera.Zoom;
            topRight /= camera.Zoom;
            bottomLeft /= camera.Zoom;
            bottomRight /= camera.Zoom;
        }

        ContainedRoundedRectangle.CornerRadius = cornerRadius;
        ContainedRoundedRectangle.CustomRadiusTopLeft = topLeft;
        ContainedRoundedRectangle.CustomRadiusTopRight = topRight;
        ContainedRoundedRectangle.CustomRadiusBottomLeft = bottomLeft;
        ContainedRoundedRectangle.CustomRadiusBottomRight = bottomRight;

        // Issue #2814: mirror corner radii onto the stroke slot when two-slot composition
        // is engaged so the outline traces the same rounded corners as the fill.
        if (StrokeRenderable is RoundedRectangle strokeRounded)
        {
            strokeRounded.CornerRadius = cornerRadius;
            strokeRounded.CustomRadiusTopLeft = topLeft;
            strokeRounded.CustomRadiusTopRight = topRight;
            strokeRounded.CustomRadiusBottomLeft = bottomLeft;
            strokeRounded.CustomRadiusBottomRight = bottomRight;
        }
        base.PreRender();
    }
#endif
}
