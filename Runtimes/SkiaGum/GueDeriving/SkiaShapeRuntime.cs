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

/// <summary>
/// Base class for Skia shape runtimes (Circle, RoundedRectangle, Arc, Polygon, ColoredCircle,
/// etc.). Holds the cross-cutting solid/gradient/stroke/dropshadow accessors that pass through
/// to the underlying <see cref="RenderableShapeBase"/>, and (since issue #2790) provides the
/// two-slot fill+stroke composition model for derived runtimes that want to draw both layers
/// of a shape simultaneously.
/// </summary>
/// <remarks>
/// <para><b>Two-slot composition recipe (issue #2790)</b></para>
/// <para>
/// A derived runtime opts in to two-slot composition so a single instance can render a filled
/// disk AND a stroked outline at the same time, matching the XNA-like backends' <c>_fill</c> +
/// <c>_stroke</c> model. The Skia renderable is single-slot (one <c>RenderableShapeBase</c>
/// chooses fill or stroke via <c>IsFilled</c>), so composition is built up at the runtime
/// layer by wiring two renderable instances:
/// </para>
/// <list type="number">
/// <item>
/// In the runtime's constructor, after <c>SetContainedObject(fillSlot)</c>, call
/// <see cref="SetStrokeRenderable"/> with a second renderable instance. The base wires it as
/// a child of the fill so the Skia renderer draws fill first, then stroke on top.
/// </item>
/// <item>
/// Override <see cref="GraphicalUiElement.Clone"/> on the derived runtime: call
/// <see cref="ClearStrokeRenderable"/> on the clone (its <see cref="StrokeRenderable"/>
/// field was shallow-copied via MemberwiseClone and still points at the source's stroke),
/// then <see cref="SetStrokeRenderable"/> with a fresh instance. See
/// <c>CircleRuntime.Clone</c> (Skia branch) for the canonical example.
/// </item>
/// <item>
/// User-facing color routing is automatic: <see cref="FillColor"/> writes to the fill slot,
/// <see cref="StrokeColor"/> writes to the stroke slot. Either can be set to <c>null</c> to
/// hide that slot independently.
/// </item>
/// <item>
/// Stroke-flavored properties (<see cref="StrokeWidth"/>, <see cref="StrokeDashLength"/>,
/// <see cref="StrokeGapLength"/>) route to <see cref="StrokeTarget"/>, which prefers the
/// stroke slot when two-slot is engaged.
/// </item>
/// <item>
/// Gradient on a slot is silenced when the slot's color is <c>null</c> — see
/// <c>RefreshSlotGradients</c>. Without this gate <c>SKPaint.Shader</c> would draw a gradient
/// over the alpha-0 color anyway, defeating the "null = invisible" contract.
/// </item>
/// <item>
/// Dropshadow is live-routed each frame in <see cref="PreRender"/> via
/// <c>DropshadowTarget</c> (fill if FillColor is set, else stroke). Drawing the shadow on
/// both slots would visibly double up.
/// </item>
/// <item>
/// PreRender mirrors the runtime's <c>Width</c> / <c>Height</c> onto the stroke slot each
/// frame because the stroke is a plain renderable (not a layout-aware
/// <see cref="GraphicalUiElement"/>) and won't auto-track its parent's size.
/// </item>
/// </list>
/// <para>
/// Runtimes that don't opt in stay on the legacy single-slot model where fill and stroke
/// share one renderable's color + IsFilled toggle (last non-null setter wins). Most existing
/// Skia shape runtimes (RoundedRectangle, Arc, Polygon, ColoredCircle, Line, LineGrid) are
/// still single-slot; opting them in is a per-runtime change following the recipe above.
/// </para>
/// <para>
/// The MG/XNA-like backends use a different mechanism — <c>RenderableRegistry</c> resolves
/// fill and stroke slots via factories at construction (see <c>AposShapeRuntime</c>) — but
/// the resulting runtime API surface is identical, by design.
/// </para>
/// </remarks>
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
        if (StrokeRenderable != null)
        {
            // Two-slot: each slot is gated independently so a gradient lights up only where the
            // user lit up a color (issue #2790). Issue #2938: fill activity is now gated by
            // IsFilled (FillColor is non-nullable), stroke activity by StrokeWidth > 0.
            ContainedRenderable.UseGradient = _useGradient && _isFilled;
            StrokeRenderable.UseGradient = _useGradient && StrokeWidth > 0;
        }
        else
        {
            // Single-slot: the contained renderable IS the active slot regardless of whether
            // the user routed through the legacy Color setter, FillColor, or StrokeColor.
            // Gating here on FillColor / IsFilled silently suppressed gradients on every
            // single-slot shape used via the legacy Color API (Arc, RoundedRectangle, Polygon,
            // ColoredCircle, Line, LineGrid) — that was a regression from #2790; this branch
            // restores the pre-#2790 pass-through.
            ContainedRenderable.UseGradient = _useGradient;
        }
    }

    // Issue #3009 — in two-slot mode (Circle/Rectangle) the gradient START stop is the slot's own
    // solid body color: the fill slot mirrors FillColor, the stroke slot mirrors StrokeColor into
    // its Red1/Green1/Blue1/Alpha1. This removes the solid↔gradient color jump when toggling
    // UseGradient (the start already equals the color the shape was showing) and converges the
    // dropshadow alpha onto the gradient start. Legacy single-slot shapes (Arc, RoundedRectangle,
    // ColoredCircle, Polygon, ...) are NOT two-slot, so they keep their explicit standalone Color1
    // (set through the base Color1 / Red1.. members) and this is a no-op for them.
    void SyncGradientStartToBody()
    {
        if (StrokeRenderable == null)
        {
            return;
        }
        ContainedRenderable.Red1 = _fillColor.Red;
        ContainedRenderable.Green1 = _fillColor.Green;
        ContainedRenderable.Blue1 = _fillColor.Blue;
        ContainedRenderable.Alpha1 = _fillColor.Alpha;

        StrokeRenderable.Red1 = _strokeColor.Red;
        StrokeRenderable.Green1 = _strokeColor.Green;
        StrokeRenderable.Blue1 = _strokeColor.Blue;
        StrokeRenderable.Alpha1 = _strokeColor.Alpha;
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

    // Issue #2938 regression fix: defaults to transparent (alpha 0) so a freshly-constructed
    // shape runtime renders as a stroke-only outline (matching pre-#2938 visual). IsFilled is
    // true by default so the gate is open — assigning FillColor to a visible color lights up
    // the fill without flipping IsFilled.
    SKColor _fillColor = new SKColor(0, 0, 0, 0);

    /// <summary>
    /// Color of the filled disk/shape. Non-nullable since issue #2938 — use <see cref="IsFilled"/>
    /// to hide the fill rather than nulling the color. When <see cref="IsFilled"/> is <c>true</c>
    /// (the default) the fill slot renders with this color; when <c>false</c> the fill slot's
    /// alpha is forced to 0 while the backing color round-trips so toggling <see cref="IsFilled"/>
    /// back on restores the previously-set color. Defaults to transparent (alpha 0) preserving
    /// the historical stroke-only ctor visual.
    /// </summary>
    /// <remarks>
    /// When the runtime opts into two-slot composition (issue #2790) via
    /// <see cref="SetStrokeRenderable"/>, <see cref="FillColor"/> writes only to the dedicated
    /// fill slot and renders simultaneously with <see cref="StrokeColor"/>. When the runtime
    /// stays on the legacy single-slot model, fill and stroke share the contained renderable's
    /// color + IsFilled toggle.
    /// </remarks>
    public SKColor FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            PushFillColorToSlot();
            RefreshSlotGradients();
        }
    }

    /// <summary>Red channel of <see cref="FillColor"/>.</summary>
    public int FillRed
    {
        get => _fillColor.Red;
        set => FillColor = new SKColor((byte)value, _fillColor.Green, _fillColor.Blue, _fillColor.Alpha);
    }

    /// <summary>Green channel of <see cref="FillColor"/>.</summary>
    public int FillGreen
    {
        get => _fillColor.Green;
        set => FillColor = new SKColor(_fillColor.Red, (byte)value, _fillColor.Blue, _fillColor.Alpha);
    }

    /// <summary>Blue channel of <see cref="FillColor"/>.</summary>
    public int FillBlue
    {
        get => _fillColor.Blue;
        set => FillColor = new SKColor(_fillColor.Red, _fillColor.Green, (byte)value, _fillColor.Alpha);
    }

    /// <summary>Alpha channel of <see cref="FillColor"/>.</summary>
    public int FillAlpha
    {
        get => _fillColor.Alpha;
        set => FillColor = new SKColor(_fillColor.Red, _fillColor.Green, _fillColor.Blue, (byte)value);
    }

    void PushFillColorToSlot()
    {
        SKColor pushed = _isFilled ? _fillColor : new SKColor(0, 0, 0, 0);
        if (StrokeRenderable != null)
        {
            // Two-slot: fill slot is locked to IsFilled = true; IsFilled = false alpha-0s the
            // fill slot while leaving the stroke slot untouched.
            ContainedRenderable.IsFilled = true;
            ContainedRenderable.Color = pushed;
        }
        else
        {
            // Single-slot legacy model: fill and stroke share the contained renderable.
            ContainedRenderable.IsFilled = _isFilled;
            ContainedRenderable.Color = pushed;
        }
        // Issue #3009 — keep each slot's gradient start in lockstep with its body color (two-slot only).
        SyncGradientStartToBody();
    }

    SKColor _strokeColor = SKColors.White;

    /// <summary>
    /// Color of the stroked outline. Non-nullable since issue #2938 — use <see cref="StrokeWidth"/>
    /// set to 0 to hide the stroke rather than nulling the color. In two-slot mode the stroke
    /// slot always renders this color; visibility is gated by <see cref="StrokeWidth"/> at draw
    /// time.
    /// </summary>
    /// <remarks>
    /// When the runtime opts into two-slot composition (issue #2790) via
    /// <see cref="SetStrokeRenderable"/>, <see cref="StrokeColor"/> writes only to the dedicated
    /// stroke slot and renders simultaneously with <see cref="FillColor"/>. When the runtime
    /// stays on the legacy single-slot model, fill and stroke share the contained renderable's
    /// color + IsFilled toggle.
    /// </remarks>
    public SKColor StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            if (StrokeRenderable != null)
            {
                StrokeRenderable.Color = value;
            }
            else
            {
                ContainedRenderable.IsFilled = false;
                ContainedRenderable.Color = value;
            }
            // Issue #3009 — keep the stroke slot's gradient start in lockstep with StrokeColor (two-slot only).
            SyncGradientStartToBody();
            RefreshSlotGradients();
        }
    }

    /// <summary>Red channel of <see cref="StrokeColor"/>.</summary>
    public int StrokeRed
    {
        get => _strokeColor.Red;
        set => StrokeColor = new SKColor((byte)value, _strokeColor.Green, _strokeColor.Blue, _strokeColor.Alpha);
    }

    /// <summary>Green channel of <see cref="StrokeColor"/>.</summary>
    public int StrokeGreen
    {
        get => _strokeColor.Green;
        set => StrokeColor = new SKColor(_strokeColor.Red, (byte)value, _strokeColor.Blue, _strokeColor.Alpha);
    }

    /// <summary>Blue channel of <see cref="StrokeColor"/>.</summary>
    public int StrokeBlue
    {
        get => _strokeColor.Blue;
        set => StrokeColor = new SKColor(_strokeColor.Red, _strokeColor.Green, (byte)value, _strokeColor.Alpha);
    }

    /// <summary>Alpha channel of <see cref="StrokeColor"/>.</summary>
    public int StrokeAlpha
    {
        get => _strokeColor.Alpha;
        set => StrokeColor = new SKColor(_strokeColor.Red, _strokeColor.Green, _strokeColor.Blue, (byte)value);
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

    bool _isFilled = true;

    /// <summary>
    /// Gates fill rendering. When <c>true</c> (the default since issue #2938) the fill slot is
    /// painted with <see cref="FillColor"/>; when <c>false</c> the fill slot's alpha is forced
    /// to 0 so only the stroke draws. Mirrors the XNALIKE <c>CircleRuntime.IsFilled</c> gate.
    /// Stroke visibility is gated separately by <see cref="StrokeWidth"/> (0 hides stroke).
    /// </summary>
    public bool IsFilled
    {
        get => _isFilled;
        set
        {
            _isFilled = value;
            PushFillColorToSlot();
            RefreshSlotGradients();
        }
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

    /// <summary>
    /// Composite read/write for the dropshadow color, mirroring the same-named property on
    /// the MonoGame <c>AposShapeRuntime</c> so cross-backend sample code can set the
    /// dropshadow color in one assignment instead of four per-channel writes.
    /// </summary>
    public SKColor DropshadowColor
    {
        get => _dropshadowColor;
        set => _dropshadowColor = value;
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
    /// <summary>
    /// Horizontal blur radius of the dropshadow, in pixels. This is the VISIBLE blur radius —
    /// roughly how far the soft falloff extends outward from the shape silhouette. A value of
    /// 0 produces a hard-edged shadow; larger values produce a softer, wider halo.
    /// </summary>
    /// <remarks>
    /// <para>Skia is the authoritative renderer for this property. The Skia paint code at
    /// <c>RenderableShapeBase.CreatePaint</c> (Runtimes/SkiaGum/Renderables/RenderableShapeBase.cs)
    /// passes <c>DropshadowBlurX / 3.0f</c> to <c>SKImageFilter.CreateDropShadow</c> as the
    /// Gaussian sigma. A Gaussian's visible falloff extends to roughly 3σ, so the visible blur
    /// radius works back out to the user-set <see cref="DropshadowBlurX"/>.</para>
    /// <para>Other backends approximate the same visible extent without a true Gaussian shader.
    /// Treat the user-set value as "how far the shadow visibly bleeds," not as sigma — sigma
    /// would require multiplying by 3 to get the visible radius.</para>
    /// </remarks>
    public float DropshadowBlurX
    {
        get => _dropshadowBlurX;
        set => _dropshadowBlurX = value;
    }

    float _dropshadowBlurY;
    /// <inheritdoc cref="DropshadowBlurX"/>
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
        StrokeRenderable == null || _isFilled
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
