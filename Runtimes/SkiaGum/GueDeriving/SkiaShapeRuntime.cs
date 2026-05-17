using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaSharp;
using System;

#if FRB
namespace SkiaGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

public abstract class SkiaShapeRuntime : InteractiveGue
{
    protected abstract RenderableShapeBase ContainedRenderable { get; }

    /// <summary>
    /// Optional second renderable used to draw the stroke when a runtime opts into the two-slot
    /// fill+stroke composition model (issue #2790). When non-null, <see cref="ContainedRenderable"/>
    /// acts as the fill slot and this instance — added as a child of the fill so the renderer
    /// draws fill before stroke — acts as the stroke slot. When null, the runtime stays on the
    /// legacy single-slot model: stroke and fill share the contained renderable's color + IsFilled
    /// toggle, last non-null setter wins.
    /// </summary>
    protected RenderableShapeBase? StrokeRenderable { get; private set; }

    /// <summary>
    /// Clears the cached stroke-slot reference, intended for derived <see cref="GraphicalUiElement.Clone"/>
    /// overrides. After cloning, the new instance's <see cref="StrokeRenderable"/> still points at
    /// the source's stroke (MemberwiseClone shallow-copies fields); the clone is then responsible
    /// for re-registering its own stroke via <see cref="SetStrokeRenderable"/>.
    /// </summary>
    protected void ClearStrokeRenderable() => StrokeRenderable = null;

    /// <summary>
    /// Renderable that stroke-flavored properties (StrokeWidth, StrokeDashLength, StrokeGapLength)
    /// route to. Resolves to <see cref="StrokeRenderable"/> when two-slot composition is engaged,
    /// otherwise to <see cref="ContainedRenderable"/> so single-slot runtimes keep their existing
    /// behavior.
    /// </summary>
    protected RenderableShapeBase StrokeTarget => StrokeRenderable ?? ContainedRenderable;

    /// <summary>
    /// Opts this runtime into two-slot fill+stroke composition (issue #2790) by registering a
    /// dedicated stroke renderable alongside the existing contained fill renderable. Must be
    /// called after <see cref="GraphicalUiElement.SetContainedObject"/> so that the fill slot
    /// exists to parent the stroke under. The stroke is forced to <c>IsFilled = false</c> and
    /// becomes a child of the fill — the renderer draws parent before children, so the visual
    /// order is fill under stroke under user-added children.
    /// </summary>
    protected void SetStrokeRenderable(RenderableShapeBase strokeRenderable)
    {
        StrokeRenderable = strokeRenderable;
        strokeRenderable.IsFilled = false;
        // Setting Parent walks the RenderableShapeBase setter which appends to fill's Children
        // collection, so the Skia Renderer's hierarchy walk reaches it after the fill draws.
        strokeRenderable.Parent = ContainedRenderable;
    }

    /// <summary>
    /// Applies <see cref="UseGradient"/> to each slot, gated by whether that slot is "active"
    /// (its color setter input is non-null). Issue #2790: keeps the API single-knob —
    /// <c>UseGradient = true</c> renders the gradient wherever the user has lit up a color, and
    /// silently does nothing on slots with <c>FillColor</c> / <c>StrokeColor</c> set to null.
    /// Without this gate, an alpha-0 slot would still draw the gradient because SKPaint.Shader
    /// overrides the paint's Color.
    /// </summary>
    void RefreshSlotGradients()
    {
        ContainedRenderable.UseGradient = _useGradient && _fillColor != null;
        if (StrokeRenderable != null)
        {
            StrokeRenderable.UseGradient = _useGradient && _strokeColor != null;
        }
    }

    #region Solid colors

    public new int Alpha
    {
        get => ContainedRenderable.Alpha;
        set => ContainedRenderable.Alpha = value;
    }

    public int Blue
    {
        get => ContainedRenderable.Blue;
        set => ContainedRenderable.Blue = value;
    }

    public int Green
    {
        get => ContainedRenderable.Green;
        set => ContainedRenderable.Green = value;
    }

    public int Red
    {
        get => ContainedRenderable.Red;
        set => ContainedRenderable.Red = value;
    }

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// writes the contained renderable's color slot directly without distinguishing fill from
    /// stroke. The new fill/stroke split (issue #2785) matches the surface of
    /// <c>CircleRuntime</c> on the XNA-likes and is the going-forward API.
    /// </summary>
    [Obsolete("Use FillColor or StrokeColor instead. See issue #2785; full fill+stroke composition arrives in #2790.")]
    public SKColor Color
    {
        get => ContainedRenderable.Color;
        set => ContainedRenderable.Color = value;
    }

    SKColor? _fillColor;

    /// <summary>
    /// Color of the filled disk/shape. When set non-null, the contained renderable switches to
    /// filled mode and renders with this color. When set null, the runtime falls back to stroke
    /// (or, if <see cref="StrokeColor"/> is also null, alpha-0 / invisible).
    /// </summary>
    /// <remarks>
    /// When the runtime opts into two-slot composition (issue #2790) via
    /// <see cref="SetStrokeRenderable"/>, <see cref="FillColor"/> writes only to the dedicated
    /// fill slot and renders simultaneously with <see cref="StrokeColor"/>. When the runtime
    /// stays on the legacy single-slot model (no stroke renderable registered), fill and stroke
    /// share the contained renderable's color + IsFilled toggle and the most-recently-set non-null
    /// value wins.
    /// </remarks>
    public SKColor? FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            if (StrokeRenderable != null)
            {
                // Two-slot: fill slot is locked to IsFilled = true; null FillColor alpha-0s it
                // while leaving the stroke slot untouched.
                ContainedRenderable.IsFilled = true;
                ContainedRenderable.Color = value ?? new SKColor(0, 0, 0, 0);
            }
            else if (value is SKColor fill)
            {
                ContainedRenderable.IsFilled = true;
                ContainedRenderable.Color = fill;
            }
            else if (_strokeColor is SKColor stroke)
            {
                // Fill cleared but a stroke is still set — keep the stroke visible.
                ContainedRenderable.IsFilled = false;
                ContainedRenderable.Color = stroke;
            }
            else
            {
                // Neither set — hide via alpha 0 so the runtime is fully invisible without
                // tearing down the renderable.
                ContainedRenderable.Color = new SKColor(0, 0, 0, 0);
            }
            RefreshSlotGradients();
        }
    }

    SKColor? _strokeColor;

    /// <summary>
    /// Color of the stroked outline. When set non-null, the contained renderable switches to
    /// stroke mode and renders with this color. When set null, the runtime falls back to fill
    /// (or, if <see cref="FillColor"/> is also null, alpha-0 / invisible).
    /// </summary>
    /// <remarks>
    /// When the runtime opts into two-slot composition (issue #2790) via
    /// <see cref="SetStrokeRenderable"/>, <see cref="StrokeColor"/> writes only to the dedicated
    /// stroke slot and renders simultaneously with <see cref="FillColor"/>. When the runtime
    /// stays on the legacy single-slot model, fill and stroke share the contained renderable's
    /// color + IsFilled toggle.
    /// </remarks>
    public SKColor? StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            if (StrokeRenderable != null)
            {
                // Two-slot: stroke slot is locked to IsFilled = false; null StrokeColor alpha-0s
                // it while leaving the fill slot untouched.
                StrokeRenderable.Color = value ?? new SKColor(0, 0, 0, 0);
            }
            else if (value is SKColor stroke)
            {
                ContainedRenderable.IsFilled = false;
                ContainedRenderable.Color = stroke;
            }
            else if (_fillColor is SKColor fill)
            {
                ContainedRenderable.IsFilled = true;
                ContainedRenderable.Color = fill;
            }
            else
            {
                ContainedRenderable.Color = new SKColor(0, 0, 0, 0);
            }
            RefreshSlotGradients();
        }
    }
    #endregion

    #region Gradient Colors

    // Issue #2790 — gradient props mirror to BOTH slots (fill + stroke) when two-slot is engaged
    // so a single UseGradient toggle applies the gradient wherever the user has lit up a color.
    // The "active slot" gating lives in RefreshSlotGradients (driven by FillColor / StrokeColor
    // null-ness), not here — letting users set gradient endpoints up front before deciding which
    // slots will render.

    void ApplyToBothSlots(Action<RenderableShapeBase> apply)
    {
        apply(ContainedRenderable);
        if (StrokeRenderable != null) apply(StrokeRenderable);
    }

    public int Blue1
    {
        get => ContainedRenderable.Blue1;
        set => ApplyToBothSlots(r => r.Blue1 = value);
    }

    public int Green1
    {
        get => ContainedRenderable.Green1;
        set => ApplyToBothSlots(r => r.Green1 = value);
    }

    public int Red1
    {
        get => ContainedRenderable.Red1;
        set => ApplyToBothSlots(r => r.Red1 = value);
    }

    public int Alpha1
    {
        get => ContainedRenderable.Alpha1;
        set => ApplyToBothSlots(r => r.Alpha1 = value);
    }

    public SKColor Color1
    {
        get => new SKColor((byte)Red1, (byte)Green1, (byte)Blue1, (byte)Alpha1);
        set
        {
            Red1 = value.Red;
            Green1 = value.Green;
            Blue1 = value.Blue;
            Alpha1 = value.Alpha;
        }
    }


    public int Blue2
    {
        get => ContainedRenderable.Blue2;
        set => ApplyToBothSlots(r => r.Blue2 = value);
    }

    public int Green2
    {
        get => ContainedRenderable.Green2;
        set => ApplyToBothSlots(r => r.Green2 = value);
    }

    public int Red2
    {
        get => ContainedRenderable.Red2;
        set => ApplyToBothSlots(r => r.Red2 = value);
    }

    public int Alpha2
    {
        get => ContainedRenderable.Alpha2;
        set => ApplyToBothSlots(r => r.Alpha2 = value);
    }

    public SKColor Color2
    {
        get => new SKColor((byte)Red2, (byte)Green2, (byte)Blue2, (byte)Alpha2);
        set
        {
            Red2 = value.Red;
            Green2 = value.Green;
            Blue2 = value.Blue;
            Alpha2 = value.Alpha;
        }
    }

    public float GradientX1
    {
        get => ContainedRenderable.GradientX1;
        set => ApplyToBothSlots(r => r.GradientX1 = value);
    }
    public GeneralUnitType GradientX1Units
    {
        get => ContainedRenderable.GradientX1Units;
        set => ApplyToBothSlots(r => r.GradientX1Units = value);
    }
    public float GradientY1
    {
        get => ContainedRenderable.GradientY1;
        set => ApplyToBothSlots(r => r.GradientY1 = value);
    }
    public GeneralUnitType GradientY1Units
    {
        get => ContainedRenderable.GradientY1Units;
        set => ApplyToBothSlots(r => r.GradientY1Units = value);
    }

    public float GradientX2
    {
        get => ContainedRenderable.GradientX2;
        set => ApplyToBothSlots(r => r.GradientX2 = value);
    }
    public GeneralUnitType GradientX2Units
    {
        get => ContainedRenderable.GradientX2Units;
        set => ApplyToBothSlots(r => r.GradientX2Units = value);
    }
    public float GradientY2
    {
        get => ContainedRenderable.GradientY2;
        set => ApplyToBothSlots(r => r.GradientY2 = value);
    }
    public GeneralUnitType GradientY2Units
    {
        get => ContainedRenderable.GradientY2Units;
        set => ApplyToBothSlots(r => r.GradientY2Units = value);
    }

    bool _useGradient;

    /// <summary>
    /// When <c>true</c>, the gradient color/coordinate properties drive rendering instead of
    /// <see cref="FillColor"/> / <see cref="StrokeColor"/>. Issue #2790: applies independently
    /// to whichever slots are active — a slot whose color (<see cref="FillColor"/> or
    /// <see cref="StrokeColor"/>) is <c>null</c> stays invisible regardless of this flag.
    /// </summary>
    public bool UseGradient
    {
        get => _useGradient;
        set
        {
            _useGradient = value;
            RefreshSlotGradients();
        }
    }

    public GradientType GradientType
    {
        get => ContainedRenderable.GradientType;
        set => ApplyToBothSlots(r => r.GradientType = value);
    }

    public float GradientInnerRadius
    {
        get => ContainedRenderable.GradientInnerRadius;
        set => ApplyToBothSlots(r => r.GradientInnerRadius = value);
    }

    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => ContainedRenderable.GradientInnerRadiusUnits;
        set => ApplyToBothSlots(r => r.GradientInnerRadiusUnits = value);
    }

    public float GradientOuterRadius
    {
        get => ContainedRenderable.GradientOuterRadius;
        set => ApplyToBothSlots(r => r.GradientOuterRadius = value);
    }

    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => ContainedRenderable.GradientOuterRadiusUnits;
        set => ApplyToBothSlots(r => r.GradientOuterRadiusUnits = value);
    }



    #endregion

    #region Filled/Stroke

    /// <summary>
    /// Obsolete: use <see cref="FillColor"/> or <see cref="StrokeColor"/>. Legacy member that
    /// flips the contained renderable between filled and stroked modes. The new fill/stroke
    /// split (issue #2785) supersedes this single-toggle by letting the caller specify the
    /// color directly per slot.
    /// </summary>
    [Obsolete("Use FillColor or StrokeColor instead. See issue #2785; full fill+stroke composition arrives in #2790.")]
    public bool IsFilled
    {
        get => ContainedRenderable.IsFilled;
        set => ContainedRenderable.IsFilled = value;
    }

    // This should NOT modify the contained renderable stroke width directly.
    // Rather it should be a value that does not affect the underlying object until
    // pre-render happens where the StrokeWidthUnits can be adjusted too:
    public float StrokeWidth
    {
        get;
        set;
    }

    public DimensionUnitType StrokeWidthUnits
    {
        get;
        set;
    }

    /// <summary>
    /// Pass-through to the contained renderable's anti-aliasing flag (issue #2798). Mirrors
    /// the property of the same name on the MonoGame <c>CircleRuntime</c>, which routes
    /// through <c>IAntialiasedRenderable</c>; on Skia the contained renderable is always
    /// AA-capable so the value pushes straight through. Issue #2790: when two-slot
    /// composition is engaged, the value is mirrored to the stroke slot too so a user can
    /// flip a dashed/dotted ring to crisp pixels (Win95-style) without the override silently
    /// not reaching the slot that's actually drawing the stroke.
    /// </summary>
    public bool IsAntialiased
    {
        get => ContainedRenderable.IsAntialiased;
        set
        {
            ContainedRenderable.IsAntialiased = value;
            if (StrokeRenderable != null) StrokeRenderable.IsAntialiased = value;
        }
    }

    public float StrokeDashLength
    {
        get => StrokeTarget.StrokeDashLength;
        set => StrokeTarget.StrokeDashLength = value;
    }

    public float StrokeGapLength
    {
        get => StrokeTarget.StrokeGapLength;
        set => StrokeTarget.StrokeGapLength = value;
    }

    #endregion

    #region Dropshadow

    // Issue #2790 — dropshadow lives on backing fields and is pushed to a chosen slot in
    // PreRender so the shadow follows the user's intent: fill if FillColor is set (shadow
    // renders behind the disk and reads through any stroke on top), otherwise stroke (a
    // stroke-only ring still casts a shadow). Pushing to both would draw the shadow twice
    // and double up visibly. Single-slot legacy: the target is always ContainedRenderable.

    SKColor _dropshadowColor;

    public int DropshadowAlpha
    {
        get => _dropshadowColor.Alpha;
        set => _dropshadowColor = new SKColor(_dropshadowColor.Red, _dropshadowColor.Green, _dropshadowColor.Blue, (byte)value);
    }

    public int DropshadowBlue
    {
        get => _dropshadowColor.Blue;
        set => _dropshadowColor = new SKColor(_dropshadowColor.Red, _dropshadowColor.Green, (byte)value, _dropshadowColor.Alpha);
    }

    public int DropshadowGreen
    {
        get => _dropshadowColor.Green;
        set => _dropshadowColor = new SKColor(_dropshadowColor.Red, (byte)value, _dropshadowColor.Blue, _dropshadowColor.Alpha);
    }

    public int DropshadowRed
    {
        get => _dropshadowColor.Red;
        set => _dropshadowColor = new SKColor((byte)value, _dropshadowColor.Green, _dropshadowColor.Blue, _dropshadowColor.Alpha);
    }

    bool _hasDropshadow;
    public bool HasDropshadow
    {
        get => _hasDropshadow;
        set => _hasDropshadow = value;
    }

    float _dropshadowOffsetX;
    public float DropshadowOffsetX
    {
        get => _dropshadowOffsetX;
        set => _dropshadowOffsetX = value;
    }

    float _dropshadowOffsetY;
    public float DropshadowOffsetY
    {
        get => _dropshadowOffsetY;
        set => _dropshadowOffsetY = value;
    }

    float _dropshadowBlurX;
    public float DropshadowBlurX
    {
        get => _dropshadowBlurX;
        set => _dropshadowBlurX = value;
    }

    float _dropshadowBlurY;
    public float DropshadowBlurY
    {
        get => _dropshadowBlurY;
        set => _dropshadowBlurY = value;
    }

    /// <summary>
    /// Renderable that receives the dropshadow each frame in <see cref="PreRender"/>. Prefer
    /// fill when FillColor is set (so the shadow renders underneath any stroke layered on top);
    /// otherwise fall back to stroke. Single-slot runtimes always pick the contained renderable.
    /// </summary>
    RenderableShapeBase DropshadowTarget =>
        StrokeRenderable == null || _fillColor != null
            ? ContainedRenderable
            : StrokeRenderable;

    void ApplyDropshadow()
    {
        RenderableShapeBase target = DropshadowTarget;
        target.HasDropshadow = _hasDropshadow;
        target.DropshadowColor = _dropshadowColor;
        target.DropshadowOffsetX = _dropshadowOffsetX;
        target.DropshadowOffsetY = _dropshadowOffsetY;
        target.DropshadowBlurX = _dropshadowBlurX;
        target.DropshadowBlurY = _dropshadowBlurY;

        // Clear shadow on the non-target slot so a target switch (e.g. user nulls FillColor)
        // doesn't leave a stale shadow rendering on the previous owner.
        if (StrokeRenderable != null)
        {
            RenderableShapeBase other = target == ContainedRenderable
                ? StrokeRenderable
                : ContainedRenderable;
            other.HasDropshadow = false;
        }
    }

    #endregion

    /// <summary>
    /// Passthrough to <see cref="GraphicalUiElement.SetContainedObject"/>. Exists for symmetry
    /// with <c>AposShapeRuntime.SetContainedShape</c>, which also hooks the renderable's PreRender
    /// callback so unit-bearing properties (e.g. ScreenPixel stroke width) re-resolve each frame.
    /// Skia doesn't need that hook, so this overload just forwards. Having the same method name
    /// available on both runtimes lets unified shape-runtime files (e.g. RoundedRectangleRuntime)
    /// share one constructor without #if-gating the contained-object setup.
    /// </summary>
    protected void SetContainedShape(RenderableShapeBase shape)
    {
        SetContainedObject(shape);
    }

    public override void PreRender()
    {
        var strokeWidth = StrokeWidth;

        switch (StrokeWidthUnits)
        {
            case DimensionUnitType.Absolute:
                // do nothing
                break;
            case DimensionUnitType.ScreenPixel:
                if (this.EffectiveManagers != null)
                {
                    var camera = this.EffectiveManagers.Renderer.Camera;
                    strokeWidth /= camera.Zoom;
                }
                break;
        }

        StrokeTarget.StrokeWidth = strokeWidth;

        // Two-slot composition (#2790): the stroke renderable is a plain child of the fill —
        // layout pushes Width/Height onto the fill (via SetContainedObject) but not through to
        // the stroke. Mirror the dimensions each frame so the stroke ring stays inscribed inside
        // the runtime's bounds (RenderableShapeBase.IsOffsetAppliedForStroke handles the
        // half-stroke inset inside the renderer).
        if (StrokeRenderable != null)
        {
            StrokeRenderable.Width = ContainedRenderable.Width;
            StrokeRenderable.Height = ContainedRenderable.Height;
        }

        ApplyDropshadow();

        base.PreRender();
    }
}
