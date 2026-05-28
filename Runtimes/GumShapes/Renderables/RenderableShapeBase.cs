using Apos.Shapes;
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;

namespace MonoGameAndGum.Renderables;

// Issue #2768 two-slot model: the role contracts (IFilledCircleRenderable,
// IStrokedCircleRenderable, IFilledRectangleRenderable, IStrokedRectangleRenderable) are
// implemented on the concrete shape classes (Circle, RoundedRectangle) directly, not the
// shared base. The base no longer participates in any renderable-registry contract — only
// the concrete shape classes do.
public abstract class RenderableShapeBase : RenderableBase, Gum.GueDeriving.IBlendedRenderable
{
    protected ShapeRenderer ShapeRenderer => ShapeRenderer.Self;


    // this is the default in Skia renderables so use that here:
    public Color Color { get; set; } = Color.Red;

    /// <summary>
    /// When <c>true</c> (the default) the shape's edge is rendered with 1 px of anti-aliasing.
    /// When <c>false</c> the AA radius is dropped to 0 and the edge rasterizes crisply — useful
    /// for retro / pixel-art themes (Win95 dotted focus rect, hairlines, marching ants) where
    /// AA bloom widens a nominal 1 px stroke and erodes 1 px dash/gap patterns. Mirrored on the
    /// runtime as <c>AposShapeRuntime.IsAntialiased</c>; the runtime pushes its value here in
    /// PreRender each frame.
    /// </summary>
    public bool IsAntialiased { get; set; } = true;
    public int Alpha
    {
        get => Color.A;
        set
        {
            this.Color = new Color(this.Color.R, this.Color.G, this.Color.B, (byte)value);
        }
    }

    public int Blue
    {
        get => Color.B;
        set
        {
            this.Color = new Color(this.Color.R, this.Color.G, (byte)value, this.Color.A);
        }
    }

    public int Green
    {
        get => Color.G;
        set
        {
            this.Color = new Color(this.Color.R, (byte)value, this.Color.B, this.Color.A);
        }
    }

    public int Red
    {
        get => Color.R;
        set
        {
            this.Color = new Color((byte)value, this.Color.G, this.Color.B, this.Color.A);
        }
    }

    #region Blend

    /// <summary>
    /// Issue #2937 — the blend mode used when this shape is drawn. Mirrors the Blend variable
    /// surfaced on plain Circle/Rectangle (PR #2933) and the other shape runtimes. Default
    /// <see cref="Gum.RenderingLibrary.Blend.Normal"/> preserves Apos.Shapes' historical
    /// AlphaBlend rendering — see <see cref="GetEffectiveXnaBlendState"/>. The blend is NOT part
    /// of <see cref="BatchKey"/> (that names the rendering tech, not internal state); instead the
    /// shared <see cref="ShapeRenderer"/> re-opens the ShapeBatch with this blend when a shape
    /// draws with one different from the batch's current blend — see <c>ShapeRenderer.EnsureBlend</c>.
    /// </summary>
    public Gum.RenderingLibrary.Blend Blend { get; set; } = Gum.RenderingLibrary.Blend.Normal;

    /// <summary>
    /// Resolves <see cref="Blend"/> to the XNA <see cref="Microsoft.Xna.Framework.Graphics.BlendState"/>
    /// handed to Apos.Shapes' <c>ShapeBatch.Begin</c> in <see cref="StartBatch"/>. Returns
    /// <c>null</c> for <see cref="Gum.RenderingLibrary.Blend.Normal"/> so <c>Begin</c> keeps its
    /// own <c>AlphaBlend</c> default — the blend every Apos shape has rendered with since before
    /// this property existed — leaving existing content visually unchanged. Only an explicitly
    /// non-Normal blend (Additive, etc.) overrides it.
    /// </summary>
    public Microsoft.Xna.Framework.Graphics.BlendState? GetEffectiveXnaBlendState()
    {
        if (Blend == Gum.RenderingLibrary.Blend.Normal)
        {
            return null;
        }
        return Gum.RenderingLibrary.BlendExtensions.ToBlendState(Blend).ToXNA();
    }

    #endregion

    #region Gradient

    private bool _useGradient;
    public bool UseGradient
    {
        get => _useGradient;
        set
        {
            _useGradient = value;
        }
    }

    private GradientType _gradientType;
    public GradientType GradientType
    {
        get => _gradientType;
        set
        {
            _gradientType = value;
        }
    }

    private int _alpha1 = 255;
    public int Alpha1
    {
        get => _alpha1;
        set
        {
            _alpha1 = value;
        }
    }
    private int _red1;
    public int Red1
    {
        get => _red1; set
        {
            _red1 = value;
        }
    }
    private int _green1;
    public int Green1
    {
        get => _green1;
        set
        {
            _green1 = value;
        }
    }
    private int _blue1;
    public int Blue1
    {
        get => _blue1;
        set
        {
            _blue1 = value;
        }
    }

    private int _alpha2 = 255;
    public int Alpha2
    {
        get => _alpha2;
        set
        {
            _alpha2 = value;
        }
    }
    private int _red2;
    public int Red2
    {
        get => _red2;
        set
        {
            _red2 = value;
        }
    }
    private int _green2;
    public int Green2
    {
        get => _green2;
        set
        {
            _green2 = value;
        }
    }
    private int _blue2;
    public int Blue2
    {
        get => _blue2;
        set
        {
            _blue2 = value;
        }
    }

    private float _gradientX1;
    public float GradientX1
    {
        get => _gradientX1;
        set
        {
            _gradientX1 = value;
        }
    }
    private GeneralUnitType _gradientX1Units;
    public GeneralUnitType GradientX1Units
    {
        get => _gradientX1Units;
        set
        {
            _gradientX1Units = value;
        }
    }
    private float _gradientY1;
    public float GradientY1
    {
        get => _gradientY1;
        set
        {
            _gradientY1 = value;
        }
    }
    private GeneralUnitType _gradientY1Units;
    public GeneralUnitType GradientY1Units
    {
        get => _gradientY1Units;
        set
        {
            _gradientY1Units = value;
        }
    }

    private float _gradientX2;
    public float GradientX2
    {
        get => _gradientX2;
        set
        {
            _gradientX2 = value;
        }
    }

    private GeneralUnitType _gradientX2Units;
    public GeneralUnitType GradientX2Units
    {
        get => _gradientX2Units;
        set
        {
            _gradientX2Units = value;
        }
    }

    private float _gradientY2;
    public float GradientY2
    {
        get => _gradientY2;
        set
        {
            _gradientY2 = value;
        }
    }
    private GeneralUnitType _gradientY2Units;
    public GeneralUnitType GradientY2Units
    {
        get => _gradientY2Units;
        set
        {
            _gradientY2Units = value;
        }
    }


    private float _gradientInnerRadius;
    public float GradientInnerRadius
    {
        get => _gradientInnerRadius;
        set
        {
            _gradientInnerRadius = value;
        }
    }
    private DimensionUnitType _gradientInnerRadiusUnits;
    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => _gradientInnerRadiusUnits;
        set
        {
            _gradientInnerRadiusUnits = value;
        }
    }

    private float _gradientOuterRadius;
    public float GradientOuterRadius
    {
        get => _gradientOuterRadius;
        set
        {
            _gradientOuterRadius = value;
        }
    }
    private DimensionUnitType _gradientOuterRadiusUnits;
    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => _gradientOuterRadiusUnits;
        set
        {
            _gradientOuterRadiusUnits = value;
        }
    }

    #endregion

    #region Dropshadow

    Color _dropshadowColor;

    public Color DropshadowColor
    {
        get => _dropshadowColor;
        set
        {
            _dropshadowColor = value;
        }
    }

    /// <summary>
    /// Issue #2851 — the dropshadow color actually emitted by <see cref="Circle"/> and
    /// <see cref="RoundedRectangle"/>, with its alpha multiplied by the body's
    /// <see cref="Color"/> alpha. Matches SkiaGum (and therefore the Gum tool/viewport),
    /// where the shadow is an image filter on the same paint that draws the body, so reducing
    /// the shape's alpha fades the shadow with it. Without this scaling, an Apos.Shapes
    /// shape fading to transparent would leave an opaque shadow ghost behind.
    /// </summary>
    public Color EffectiveDropshadowColor =>
        new Color(
            _dropshadowColor.R,
            _dropshadowColor.G,
            _dropshadowColor.B,
            (byte)(_dropshadowColor.A * Color.A / 255));

    public int DropshadowAlpha
    {
        get => DropshadowColor.A;
        set
        {
            this.DropshadowColor = new Color(this.DropshadowColor.R, this.DropshadowColor.G, this.DropshadowColor.B, (byte)value);
        }
    }

    public int DropshadowBlue
    {
        get => DropshadowColor.B;
        set
        {
            this.DropshadowColor = new Color(this.DropshadowColor.R, this.DropshadowColor.G, (byte)value, this.DropshadowColor.A);
        }
    }

    public int DropshadowGreen
    {
        get => DropshadowColor.G;
        set
        {
            this.DropshadowColor = new Color(this.DropshadowColor.R, (byte)value, this.DropshadowColor.B, this.DropshadowColor.A);
        }
    }

    public int DropshadowRed
    {
        get => DropshadowColor.R;
        set
        {
            this.DropshadowColor = new Color((byte)value, this.DropshadowColor.G, this.DropshadowColor.B, this.DropshadowColor.A);
        }
    }

    private bool _hasDropshadow;

    public bool HasDropshadow
    {
        get => _hasDropshadow;
        set
        {
            _hasDropshadow = value;
        }
    }

    private float _dropshadowOffsetX;

    public float DropshadowOffsetX
    {
        get => _dropshadowOffsetX;
        set
        {
            _dropshadowOffsetX = value;
        }
    }

    private float _dropshadowOffsetY;

    public float DropshadowOffsetY
    {
        get => _dropshadowOffsetY;
        set
        {
            _dropshadowOffsetY = value;
        }
    }

    private float _dropshadowBlurX;

    /// <inheritdoc cref="SkiaGum.GueDeriving.SkiaShapeRuntime.DropshadowBlurX"/>
    /// <remarks>Apos approximates the visible falloff using the shape primitive's
    /// <c>antiAliasSize</c> parameter — no true Gaussian, but the user-set value still
    /// represents how many pixels the shadow visibly extends.</remarks>
    public float DropshadowBlurX
    {
        get => _dropshadowBlurX;
        set
        {
            // Issue #2977 — blur is a radius; a negative value is meaningless and used to make
            // the shadow vanish (a negative aaSize, which Apos.Shapes won't draw). Clamp here so
            // negative blur behaves identically to 0 for every consumer (Circle fill/stroke,
            // RoundedRectangle, Arc, gradient offsets).
            _dropshadowBlurX = System.Math.Max(0f, value);
        }
    }

    private float _dropshadowBlurY;

    /// <inheritdoc cref="DropshadowBlurX"/>
    public float DropshadowBlurY
    {
        get => _dropshadowBlurY;
        set
        {
            _dropshadowBlurY = System.Math.Max(0f, value);
        }
    }

    #endregion

    bool _isFilled = true;
    public bool IsFilled
    {
        get => _isFilled;
        set
        {
            _isFilled = value;
        }
    }

    float _strokeWidth = 2;
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            _strokeWidth = value;
        }
    }

    float _strokeDashLength;
    /// <summary>
    /// Length of each dash segment in pixels when using a dashed stroke.
    /// A value of 0 (the default) produces a solid stroke.
    /// </summary>
    public float StrokeDashLength
    {
        get => _strokeDashLength;
        set
        {
            _strokeDashLength = value;
        }
    }

    float _strokeGapLength;
    /// <summary>
    /// Length of each gap between dashes in pixels when using a dashed stroke.
    /// Ignored when <see cref="StrokeDashLength"/> is 0.
    /// </summary>
    public float StrokeGapLength
    {
        get => _strokeGapLength;
        set
        {
            _strokeGapLength = value;
        }
    }

    /// <summary>
    /// Whether this shape's current configuration would produce any visible pixels. Filled
    /// shapes always render (even with alpha-zero color — the layout is the source of truth,
    /// not visibility). Stroke-only shapes (<see cref="IsFilled"/> == <c>false</c>) need a
    /// positive <see cref="StrokeWidth"/>; with stroke width = 0 the Apos.Shapes shader would
    /// still paint a one-pixel AA fringe in the stroke color, producing a hairline ring the
    /// user thought they had disabled. <see cref="Circle.Render"/>,
    /// <see cref="RoundedRectangle.Render"/>, and <see cref="Arc.Render"/> early-return on
    /// <c>!HasVisibleOutput</c> so neither the body nor the shadow draws in that case.
    /// </summary>
    public bool HasVisibleOutput => IsFilled || StrokeWidth > 0;

    /// <summary>
    /// Returns the (effectiveRadius_world, effectiveAaSize_screenPx, alphaScale) trio to hand
    /// to Apos.Shapes' <c>DrawCircle</c> for a shadow centered on a circular shape of nominal
    /// radius <paramref name="hostRadius"/>. Anchors the smoothstep falloff so α = 0.5 sits
    /// exactly at <paramref name="hostRadius"/> and α = 0 sits at <c>hostRadius + DropshadowBlurX/2</c>
    /// — matching CSS <c>box-shadow</c> / Figma / Photoshop convention where the original
    /// disk edge is the 50% line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Apos's AA falloff is <c>3t² − 2t³</c> (smoothstep), confirmed empirically. Because
    /// smoothstep is symmetric (smoothstep(0.5) = 0.5), in the standard case (B ≤ 2R) we can
    /// just pass <c>rDisk = R − B/2, aaSize = B</c> and the 50% line lands at R for free.
    /// </para>
    /// <para>
    /// When <c>DropshadowBlurX</c> exceeds 2·hostRadius, the inner ramp edge would be at a
    /// negative radius — which Apos clamps to 0, sliding the entire ramp outward and making
    /// the 50% line drift far past R (giving the "way too big" shadow that #2950 reports).
    /// In that case the helper truncates the inner ramp at <c>rDisk = 0</c>, widens aaSize
    /// to <c>R + B/2</c> so the outer edge still sits where the desired curve says, and
    /// returns an <paramref name="alphaScale"/> &lt; 1 so the (clamped) smoothstep still
    /// passes through both anchors (R, 0.5) and (R + B/2, 0). The cost is that the center
    /// of the shadow is no longer 100% opaque — which is *correct*: a circle whose blur
    /// extends beyond its diameter genuinely should not have a solid black center.
    /// </para>
    /// <para>
    /// <paramref name="cameraZoom"/> is folded into <c>effectiveAaSize</c> (which is returned
    /// in screen pixels, since Apos's <c>aaSize</c> argument is screen-space) so the visible
    /// halo holds a constant *world* extent under zoom. Mirrors <see cref="GetShadowAntiAliasSize"/>.
    /// </para>
    /// </remarks>
    public (float effectiveRadius, int effectiveAaSize, float alphaScale)
        ComputeShadowDrawGeometry(float hostRadius, float cameraZoom)
    {
        float blur = _dropshadowBlurX;
        if (blur <= 0f)
        {
            return (hostRadius, 0, 1f);
        }
        if (blur <= 2f * hostRadius)
        {
            // Standard case: ramp fits inside the host disk. Hand Apos the symmetric
            // geometry directly; smoothstep's symmetry around t=0.5 places α=0.5 at R for us.
            return (
                hostRadius - blur * 0.5f,
                MathFunctions.RoundToInt(blur * cameraZoom),
                1f);
        }
        // blur > 2R: truncate inner edge to rDisk = 0, widen aaSize to R + B/2, and scale
        // base alpha so smoothstep(0,1, R/(R + B/2)) lands the curve at α = 0.5 at r = R.
        // alphaScale = 0.5 / (1 - smoothstep(R / aaSizeWorld)).
        float aaSizeWorld = hostRadius + blur * 0.5f;
        float t = hostRadius / aaSizeWorld;
        float smoothstep = 3f * t * t - 2f * t * t * t;
        float alphaScale = 0.5f / (1f - smoothstep);
        return (
            0f,
            MathFunctions.RoundToInt(aaSizeWorld * cameraZoom),
            alphaScale);
    }

    /// <summary>
    /// World-anchored shadow halo size, scaled by the current camera zoom and rounded to the
    /// nearest int for the Apos.Shapes <c>aaSize</c> parameter. Apos consumes <c>aaSize</c> in
    /// screen-pixel space (its shader uses <c>fwidth</c>-style pixel-derivative AA), so a raw
    /// <see cref="DropshadowBlurX"/> would render as a fixed screen-pixel count regardless of
    /// zoom — making the shadow halo shrink and shift relative to its host as the camera zooms
    /// in. Multiplying by zoom keeps the halo a constant <em>world</em> extent, matching the
    /// rest of Gum's wireframe.
    /// </summary>
    public int GetShadowAntiAliasSize(float cameraZoom)
    {
        return MathFunctions.RoundToInt(_dropshadowBlurX * cameraZoom);
    }

    /// <summary>
    /// Issue #2950 — when a stroke-only shape's dropshadow blur exceeds its stroke width, the
    /// naive <c>lineThickness = StrokeWidth - DropshadowBlurX</c> goes ≤ 0 and the Apos.Shapes
    /// shader refuses to draw, making the shadow disappear. Fix: clamp lineThickness to a small
    /// positive epsilon so Apos still draws, and scale the shadow's starting alpha by
    /// <c>StrokeWidth / DropshadowBlurX</c> so the visible band reads as the tail of the alpha
    /// ramp — "start at a smaller alpha and advance to 0" — instead of vanishing. Filled mode,
    /// blur = 0, and stroke &gt; blur all leave the values unchanged (existing behavior).
    /// </summary>
    /// <param name="baseColor">The pre-multiplied dropshadow color (typically
    /// <see cref="EffectiveDropshadowColor"/>) that would otherwise be passed to the Apos draw
    /// call before the stroke/blur fade is applied.</param>
    public (float effectiveStrokeWidth, Color effectiveColor) ComputeStrokeShadowDrawParameters(Color baseColor)
    {
        float effectiveStrokeWidth = StrokeWidth - _dropshadowBlurX;
        if (!IsFilled && effectiveStrokeWidth <= 0 && _dropshadowBlurX > 0)
        {
            float alphaScale = Microsoft.Xna.Framework.MathHelper.Clamp(StrokeWidth / _dropshadowBlurX, 0f, 1f);
            baseColor = new Color(
                baseColor.R,
                baseColor.G,
                baseColor.B,
                (byte)(baseColor.A * alphaScale));
            // Apos.Shapes treats lineThickness <= 0 as "don't draw." Push a small positive
            // epsilon so the AA band still renders. The visible falloff is driven by aaSize
            // (= DropshadowBlurX), not by this value.
            effectiveStrokeWidth = 0.01f;
        }
        return (effectiveStrokeWidth, baseColor);
    }

    /// <summary>
    /// Issue #2977 — shadow ring radius for a stroke-only shape. The filled-disk anchor model in
    /// <see cref="ComputeShadowDrawGeometry"/> pulls the radius inward by <c>blur/2</c>, which is
    /// correct for a solid disk but wrong for a ring: once blur exceeds the stroke width,
    /// <see cref="ComputeStrokeShadowDrawParameters"/> clamps the effective shadow stroke width to
    /// a ~0 epsilon, so the ring centerline collapses to <c>hostRadius - blur/2</c> and marches
    /// inward as blur grows — the shape visibly contracts. Instead anchor the shadow ring's
    /// centerline at the body stroke's centerline (<c>hostRadius - StrokeWidth/2</c>) regardless of
    /// blur; only the AA halo (aaSize) grows. Solve
    /// <c>shadowRadius - effectiveShadowStrokeWidth/2 = hostRadius - StrokeWidth/2</c> for the outer
    /// radius Apos.Shapes' <c>DrawCircle</c> expects. For <c>blur &lt; StrokeWidth</c> this reduces
    /// to the legacy <c>hostRadius - blur/2</c>, leaving the common small-blur case unchanged.
    /// </summary>
    public float ComputeStrokeShadowDrawRadius(float hostRadius, float effectiveShadowStrokeWidth)
    {
        return hostRadius - StrokeWidth / 2f + effectiveShadowStrokeWidth / 2f;
    }

    /// <summary>
    /// Issue #2956 — gates whether this renderable's gradient pass should actually draw.
    /// <see cref="UseGradient"/> is a *pattern* flag, not a *visibility* flag — a slot whose
    /// solid <see cref="Color"/> alpha is 0 (e.g. the default-transparent fill on a stroke-only
    /// plain Circle) must NOT paint its gradient. Apos.Shapes' gradient draw bypasses the
    /// slot's solid color, so without this gate it would paint an opaque gradient on a slot
    /// the user explicitly hid. SkiaSharp gets this right naturally because
    /// <c>SKPaint.Color.alpha</c> modulates the shader output; we replicate that contract
    /// here. <paramref name="forcedColor"/> is the per-call dropshadow override every render
    /// path already short-circuits on — when set, the slot is painting the shadow, not the
    /// gradient, so the gate must return false.
    /// </summary>
    public bool ShouldPaintGradient(Color? forcedColor)
    {
        return UseGradient && forcedColor == null && Color.A > 0;
    }

    /// <summary>
    /// Invoked by <see cref="PreRender"/> each frame. The wrapping <see cref="MonoGameGum.GueDeriving.AposShapeRuntime"/>
    /// hooks this so it can resolve unit-bearing properties (notably StrokeWidth with ScreenPixel units,
    /// which depends on the current camera zoom) into the renderable's plain pixel values just before drawing.
    ///
    /// This indirection exists because the renderer adds the *renderable* (this object) to the layer, not
    /// the runtime that wraps it - so the runtime's PreRender override is never reached by the renderer's
    /// PreRender walk. Without this callback, runtime-level properties like StrokeWidth never propagate to
    /// the renderable and the renderable keeps its default values.
    /// </summary>
    internal Action? OnPreRender;

    public override void PreRender()
    {
        OnPreRender?.Invoke();
    }

    protected Gradient GetGradient(float absoluteLeft, float absoluteTop, float rotationRadians = 0f)
    {
        var firstColor = new Microsoft.Xna.Framework.Color(
                (byte)Red1, (byte)Green1, (byte)Blue1, (byte)Alpha1);
        var secondColor = new Microsoft.Xna.Framework.Color(
            (byte)Red2, (byte)Green2, (byte)Blue2, (byte)Alpha2);

        var effectiveGradientX1 = absoluteLeft + GradientX1;
        switch (this.GradientX1Units)
        {
            case GeneralUnitType.PixelsFromMiddle:
                effectiveGradientX1 += Width / 2.0f;
                break;
            case GeneralUnitType.PixelsFromLarge:
                effectiveGradientX1 += Width;
                break;
            case GeneralUnitType.Percentage:
                effectiveGradientX1 = absoluteLeft + Width * GradientX1 / 100;
                break;
        }


        var effectiveGradientY1 = absoluteTop + GradientY1;
        switch (this.GradientY1Units)
        {
            case GeneralUnitType.PixelsFromMiddle:
                effectiveGradientY1 += Height / 2.0f;
                break;
            case GeneralUnitType.PixelsFromLarge:
                effectiveGradientY1 += Height;
                break;
            case GeneralUnitType.Percentage:
                effectiveGradientY1 = absoluteTop + Height * GradientY1 / 100;
                break;
        }


        // The gradient is object-space-anchored: endpoints are computed in unrotated
        // bounding-box coordinates above, then rotated around the GUE rotation pivot
        // (absoluteLeft, absoluteTop) so the gradient travels with the shape as it
        // rotates. Apos.Shapes interprets the endpoints in world coords (IsLocal=false,
        // the default) and does NOT itself rotate the gradient with the shape's rotation
        // parameter, so the rotation has to be applied here before construction.
        var pivot = new Vector2(absoluteLeft, absoluteTop);

        if(_gradientType == GradientType.Linear)
        {
            var effectiveGradientX2 = absoluteLeft + GradientX2;
            switch (this.GradientX2Units)
            {
                case GeneralUnitType.PixelsFromMiddle:
                    effectiveGradientX2 += Width / 2.0f;
                    break;
                case GeneralUnitType.PixelsFromLarge:
                    effectiveGradientX2 += Width;
                    break;
                case GeneralUnitType.Percentage:
                    effectiveGradientX2 = absoluteLeft + Width * GradientX2 / 100;
                    break;
            }
            var effectiveGradientY2 = absoluteTop + GradientY2;
            switch (this.GradientY2Units)
            {
                case GeneralUnitType.PixelsFromMiddle:
                    effectiveGradientY2 += Height / 2.0f;
                    break;
                case GeneralUnitType.PixelsFromLarge:
                    effectiveGradientY2 += Height;
                    break;
                case GeneralUnitType.Percentage:
                    effectiveGradientY2 = absoluteTop + Height * GradientY2 / 100;
                    break;
            }

            var pointA = RotateAround(new Vector2(effectiveGradientX1, effectiveGradientY1), pivot, rotationRadians);
            var pointB = RotateAround(new Vector2(effectiveGradientX2, effectiveGradientY2), pivot, rotationRadians);

            return new Gradient(pointA, firstColor, pointB, secondColor);
        }
        else
        {
            var effectiveOuterRadius = ResolveRadius(_gradientOuterRadius, _gradientOuterRadiusUnits, Width);
            var effectiveInnerRadius = ResolveRadius(_gradientInnerRadius, _gradientInnerRadiusUnits, Width);

            var effectiveGradientX2 = effectiveGradientX1 + effectiveOuterRadius;
            var effectiveGradientY2 = effectiveGradientY1;

            var pointA = RotateAround(new Vector2(effectiveGradientX1, effectiveGradientY1), pivot, rotationRadians);
            var pointB = RotateAround(new Vector2(effectiveGradientX2, effectiveGradientY2), pivot, rotationRadians);

            return new Gradient(pointA,
                firstColor,
                pointB,
                secondColor,
                s:Gradient.Shape.Radial,
                aOffset:effectiveInnerRadius);
        }
    }

    internal static float ResolveRadius(float value, DimensionUnitType units, float width)
    {
        switch (units)
        {
            case DimensionUnitType.Absolute:
                return value;
            case DimensionUnitType.PercentageOfParent:
                return width * value / 100f;
            case DimensionUnitType.RelativeToParent:
                return width + value;
            default:
                return value;
        }
    }

    /// <summary>
    /// Adjusts a top-left position so that Apos.Shapes' center-based rotation
    /// produces the same result as rotating around the top-left corner.
    /// </summary>
    protected static Vector2 AdjustPositionForCenterRotation(Vector2 topLeft, Vector2 size, float rotationRadians)
    {
        if (rotationRadians == 0)
        {
            return topLeft;
        }

        var halfW = size.X / 2.0f;
        var halfH = size.Y / 2.0f;

        // Center relative to top-left (unrotated)
        var cx = halfW;
        var cy = halfH;

        // Rotate center around origin (top-left corner)
        var cos = (float)System.Math.Cos(rotationRadians);
        var sin = (float)System.Math.Sin(rotationRadians);
        var rotatedCx = cx * cos - cy * sin;
        var rotatedCy = cx * sin + cy * cos;

        // New top-left = original top-left + rotated center - half size
        return new Vector2(
            topLeft.X + rotatedCx - halfW,
            topLeft.Y + rotatedCy - halfH);
    }

    /// <summary>
    /// Compute the absolute center of a shape whose top-left is at <paramref name="absoluteLeft"/>,
    /// <paramref name="absoluteTop"/>, taking rotation around the top-left origin into account
    /// (Gum's default rotation pivot). Issue #2925 — used by <see cref="Circle.Render"/> and
    /// <see cref="Arc.Render"/>; <see cref="RoundedRectangle"/> handles its own rotation via
    /// <c>ShapeBatch.DrawRectangle</c>'s rotation parameter and does not call this.
    /// </summary>
    /// <param name="rotationRadians">Already negated to match the rendering convention
    /// (negative of the GUE's degrees-based Rotation).</param>
    public static Vector2 GetRotatedCenter(float absoluteLeft, float absoluteTop, float width, float height, float rotationRadians)
    {
        if (rotationRadians == 0)
        {
            return new Vector2(absoluteLeft + width / 2.0f, absoluteTop + height / 2.0f);
        }

        var halfW = width / 2.0f;
        var halfH = height / 2.0f;
        var cos = (float)System.Math.Cos(rotationRadians);
        var sin = (float)System.Math.Sin(rotationRadians);
        return new Vector2(
            absoluteLeft + halfW * cos - halfH * sin,
            absoluteTop + halfW * sin + halfH * cos);
    }

    /// <summary>
    /// Rotates <paramref name="point"/> around <paramref name="pivot"/> by
    /// <paramref name="rotationRadians"/>. Shared math used by the dashed-stroke perimeter
    /// walk (RoundedRectangle) and by <see cref="GetGradient"/> when rotating gradient
    /// endpoints so the gradient stays anchored to object-local space instead of the
    /// unrotated bounding box.
    /// </summary>
    public static Vector2 RotateAround(Vector2 point, Vector2 pivot, float rotationRadians)
    {
        if (rotationRadians == 0f)
        {
            return point;
        }
        var cos = (float)System.Math.Cos(rotationRadians);
        var sin = (float)System.Math.Sin(rotationRadians);
        var dx = point.X - pivot.X;
        var dy = point.Y - pivot.Y;
        return new Vector2(
            pivot.X + dx * cos - dy * sin,
            pivot.Y + dx * sin + dy * cos);
    }

    public override string BatchKey => "Apos.Shapes";

    public override void StartBatch(ISystemManagers systemManagers)
    {
        var sb = ShapeRenderer.ShapeBatch;
        if(sb == null)
        {
            throw new InvalidOperationException(
                "ShapeRenderer is null - did you remember to call ShapeRenderer.Self.Initialize()? " +
                "For more information see documentation: https://docs.flatredball.com/gum/code/standard-visuals/shapes-apos.shapes#monogame");
        }

        // Match the view matrix that the active SpriteBatch is using so shapes scale with
        // camera zoom and any matrix passed to GumBatch.Begin. Without this, ShapeBatch
        // always renders in raw screen pixels while sprites/text track the canvas.
        var managers = systemManagers as SystemManagers;
        var spriteRenderer = managers?.Renderer?.SpriteRenderer;
        var view = spriteRenderer?.CurrentTransformMatrix;

        // Match the SpriteBatch's scissor state so shapes honor the same ClipsChildren
        // region as their sprite/text siblings. Without this, shape bodies of controls
        // placed inside a clipped container (ScrollViewer's ClipContainer, ListBox, etc.)
        // bleed past the clip region. Apos.Shapes' ShapeBatch.Begin only honors the
        // GraphicsDevice's scissor rect when its rasterizerState parameter also has
        // ScissorTestEnable=true - passing the rect alone is ignored.
        var scissor = spriteRenderer?.CurrentScissorRectangle;
        RasterizerState? rasterizerState = null;
        if (scissor.HasValue && spriteRenderer != null)
        {
            sb.GraphicsDevice.ScissorRectangle = scissor.Value.ToXNA();
            rasterizerState = spriteRenderer.ScissorTestRasterizerState;
        }

        // Issue #2937 — open the batch with this shape's blend and remember the begin
        // parameters so EnsureBlend (called from each shape's Render) can re-open the batch
        // with a different blend mid-run without changing BatchKey. This mirrors how
        // SpriteBatchStack re-Begins SpriteBatch on a blend/scissor change while keeping one
        // logical batch. GetEffectiveXnaBlendState returns null for Normal, so Begin keeps its
        // AlphaBlend default (the historical behavior).
        ShapeRenderer.BeginBatch(view, rasterizerState, this, managers?.Renderer?.RenderStateChangeStatistics);
    }

    public override void EndBatch(ISystemManagers systemManagers)
    {
        ShapeRenderer.EndBatch();
    }
}
