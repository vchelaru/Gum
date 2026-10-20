using Gum.Converters;
using Gum.DataTypes;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using SkiaSharp;
using System.Collections.ObjectModel;
using ToolsUtilitiesStandard.Helpers;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using Gum;
using System;

namespace SkiaGum.Renderables;

public class RenderableShapeBase : IRenderableIpso, IVisible, IDisposable, ICloneable
{
    /// <summary>
    /// Creates a copy for <see cref="Gum.Wireframe.GraphicalUiElement.Clone"/>. The copy has its
    /// own children collection, no parent, no <see cref="OnPreRender"/> hook, and builds its own
    /// paint on first draw. A derived shape
    /// that holds mutable reference state (such as a point list) overrides this to copy it.
    /// </summary>
    public virtual object Clone()
    {
        RenderableShapeBase clone = (RenderableShapeBase)MemberwiseClone();
        clone.mChildren = new();
        clone.mParent = null;
        // The copied paint reference belongs to the source, so drop it rather than dispose it.
        clone._cachedPaint = null;
        clone.NeedsUpdate = true;
        // The hook targets the source's runtime; the clone's runtime rebinds its own.
        clone.OnPreRender = null;
        return clone;
    }

    #region Fields/Properties

    SKColor _color = SKColors.Red;
    public SKColor Color
    {
        get => _color;
        set
        {
            _color = value;
            ClearCachedPaint();
        }
    }

    public int Alpha
    {
        get => Color.Alpha;
        set
        {
            this.Color = new SKColor(this.Color.Red, this.Color.Green, this.Color.Blue, (byte)value);
        }
    }

    public int Blue
    {
        get => Color.Blue;
        set
        {
            this.Color = new SKColor(this.Color.Red, this.Color.Green, (byte)value, this.Color.Alpha);
        }
    }

    public int Green
    {
        get => Color.Green;
        set
        {
            this.Color = new SKColor(this.Color.Red, (byte)value, this.Color.Blue, this.Color.Alpha);
        }
    }

    public int Red
    {
        get => Color.Red;
        set
        {
            this.Color = new SKColor((byte)value, this.Color.Green, this.Color.Blue, this.Color.Alpha);
        }
    }

    Vector2 Position;
    internal protected IRenderableIpso? mParent;

    public IRenderableIpso? Parent
    {
        get { return mParent; }
        set
        {
            if (mParent != value)
            {
                if (mParent != null)
                {
                    mParent.Children.Remove(this);
                }
                mParent = value;
                if (mParent != null)
                {
                    mParent.Children.Add(this);
                }
            }
        }
    }

    internal protected ObservableCollectionNoReset<IRenderableIpso> mChildren;
    public ObservableCollection<IRenderableIpso> Children
    {
        get { return mChildren; }
    }

    public float X
    {
        get => Position.X;
        set => Position.X = value;
    }

    public float Y
    {
        get { return Position.Y; }
        set { Position.Y = value; }
    }

    public float Z
    {
        get;
        set;
    }

    float _width;
    public float Width
    {
        get => _width;
        set
        {
            var changed = _width != value;
            _width = value;
            if(changed)
            {
                NeedsUpdate = true;
                if(UseGradient)
                {
                    ClearCachedPaint();
                }
            }
        }
    }

    float _height;
    public float Height
    {
        get => _height;
        set
        {
            var changed = _height != value;
            _height = value;
            if(changed)
            {
                NeedsUpdate = true;
                if(UseGradient)
                {
                    ClearCachedPaint();
                }
            }
        }
    }
    public string? Name
    {
        get;
        set;
    }
    public float Rotation { get; set; }

    public bool Wrap => false;

    public bool CanRenderAt0Dimension { get; protected set; } = false;
    public bool IsOffsetAppliedForStroke { get; set; } = true;

    /// <summary>
    /// Whether the texture generated from this shape needs to be regenerated.
    /// Set to true whenever a visual property changes.
    /// </summary>
    public bool NeedsUpdate { get; set; } = true;

    /// <summary>
    /// Extra pixels needed on each horizontal side to accommodate effects like dropshadow.
    /// </summary>
    public virtual float XSizeSpillover => HasDropshadow ? DropshadowBlurX + Math.Abs(DropshadowOffsetX) : 0;

    /// <summary>
    /// Extra pixels needed on each vertical side to accommodate effects like dropshadow.
    /// </summary>
    public virtual float YSizeSpillover => HasDropshadow ? DropshadowBlurY + Math.Abs(DropshadowOffsetY) : 0;

    /// <summary>
    /// Whether the color should be applied as a multiplier during sprite rendering.
    /// False for shapes that bake color into their paint.
    /// </summary>
    public virtual bool ShouldApplyColorOnSpriteRender => false;

    /// <summary>
    /// How <see cref="Color"/> combines with a textured renderable (<c>Sprite</c>,
    /// <c>NineSlice</c>). Modulate (the default) multiplies by it, ColorTextureAlpha uses the
    /// texture only as an alpha mask, and Add adds it on top. Shapes that bake color into their paint ignore it.
    /// </summary>
    public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;


    private bool _useGradient;
    public bool UseGradient
    {
        get => _useGradient;
        set
        {
            _useGradient = value;
            ClearCachedPaint();
        }
    }

    private GradientType _gradientType;
    public GradientType GradientType
    {
        get => _gradientType; 
        set
        {
            _gradientType = value;
            ClearCachedPaint();
        }
    }

    private int _alpha1;
    public int Alpha1
    {
        get => _alpha1; 
        set
        {
            _alpha1 = value;
            ClearCachedPaint();
        }
    }
    private int _red1;
    public int Red1
    {
        get => _red1; set
        {
            _red1 = value;
            ClearCachedPaint();
        }
    }
    private int _green1;
    public int Green1
    {
        get => _green1;
        set
        {
            _green1 = value;
            ClearCachedPaint();
        }
    }
    private int _blue1;
    public int Blue1
    {
        get => _blue1;
        set
        {
            _blue1 = value;
            ClearCachedPaint();
        }
    }

    private int _alpha2;
    public int Alpha2
    {
        get => _alpha2;
        set
        {
            _alpha2 = value;
            ClearCachedPaint();
        }
    }
    private int _red2;
    public int Red2
    {
        get => _red2;
        set
        {
            _red2 = value;
            ClearCachedPaint();
        }
    }
    private int _green2;
    public int Green2
    {
        get => _green2;
        set
        {
            _green2 = value;
            ClearCachedPaint();
        }
    }
    private int _blue2;
    public int Blue2
    {
        get => _blue2;
        set
        {
            _blue2 = value;
            ClearCachedPaint();
        }
    }

    private float _gradientX1;
    public float GradientX1
    {
        get => _gradientX1;
        set
        {
            _gradientX1 = value;
            ClearCachedPaint();
        }
    }
    private GeneralUnitType _gradientX1Units;
    public GeneralUnitType GradientX1Units
    {
        get => _gradientX1Units;
        set
        {
            _gradientX1Units = value;
            ClearCachedPaint();
        }
    }
    private float _gradientY1;
    public float GradientY1
    {
        get => _gradientY1;
        set
        {
            _gradientY1 = value;
            ClearCachedPaint();
        }
    }
    private GeneralUnitType _gradientY1Units;
    public GeneralUnitType GradientY1Units
    {
        get => _gradientY1Units;
        set
        {
            _gradientY1Units = value;
            ClearCachedPaint();
        }
    }

    private float _gradientX2;
    public float GradientX2
    {
        get => _gradientX2;
        set
        {
            _gradientX2 = value;
            ClearCachedPaint();
        }
    }

    private GeneralUnitType _gradientX2Units;
    public GeneralUnitType GradientX2Units
    {
        get => _gradientX2Units;
        set
        {
            _gradientX2Units = value;
            ClearCachedPaint();
        }
    }

    private float _gradientY2;
    public float GradientY2
    {
        get => _gradientY2;
        set
        {
            _gradientY2 = value;
            ClearCachedPaint();
        }
    }
    private GeneralUnitType _gradientY2Units;
    public GeneralUnitType GradientY2Units
    {
        get => _gradientY2Units;
        set
        {
            _gradientY2Units = value;
            ClearCachedPaint();
        }
    }


    private float _gradientInnerRadius;
    public float GradientInnerRadius
    {
        get => _gradientInnerRadius;
        set
        {
            _gradientInnerRadius = value;
            ClearCachedPaint();
        }
    }
    private DimensionUnitType _gradientInnerRadiusUnits;
    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => _gradientInnerRadiusUnits;
        set
        {
            _gradientInnerRadiusUnits = value;
            ClearCachedPaint();
        }
    }

    private float _gradientOuterRadius;
    public float GradientOuterRadius
    {
        get => _gradientOuterRadius;
        set
        {
            _gradientOuterRadius = value;
            ClearCachedPaint();
        }
    }
    private DimensionUnitType _gradientOuterRadiusUnits;
    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => _gradientOuterRadiusUnits;
        set
        {
            _gradientOuterRadiusUnits = value;
            ClearCachedPaint();
        }
    }

    bool _isDimmed;
    public bool IsDimmed
    {
        get => _isDimmed;
        set
        {
            _isDimmed = value;
            ClearCachedPaint();
        }
    }


    bool _isFilled = true;
    /// <summary>
    /// Virtual so shape-specific renderables can clamp it. <see cref="Arc"/> overrides this to
    /// always return false because Skia's <c>SKPath.AddArc</c> autocloses with a chord (a
    /// circular-segment shape nobody wants) when filled.
    /// </summary>
    public virtual bool IsFilled
    {
        get => _isFilled;
        set
        {
            _isFilled = value;
            ClearCachedPaint();
        }
    }

    bool _isAntialiased = true;
    /// <summary>
    /// When <c>true</c> (the default) edges are rendered with Skia's <c>SKPaint.IsAntialias</c>
    /// on, producing smooth edges. When <c>false</c> the paint is created without anti-aliasing
    /// for crisp / pixel-art rasterization (issue #2798). Surfaced on
    /// <see cref="Gum.GueDeriving.SkiaShapeRuntime"/> so that <c>CircleRuntime</c> etc. expose
    /// the same <c>IsAntialiased</c> property as their Apos.Shapes counterparts on MonoGame.
    /// </summary>
    public bool IsAntialiased
    {
        get => _isAntialiased;
        set
        {
            if (_isAntialiased != value)
            {
                _isAntialiased = value;
                ClearCachedPaint();
            }
        }
    }

    float _strokeWidth = 2;
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            if(value != _strokeWidth)
            {
#if FULL_DIAGNOSTICS
                if(float.IsPositiveInfinity(value))
                {
                    throw new ArgumentException("StrokeWidth cannot be set to PositiveInfinity");
                }
#endif
                _strokeWidth = value;
                ClearCachedPaint();
            }
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
            if (value != _strokeDashLength)
            {
                _strokeDashLength = value;
                ClearCachedPaint();
            }
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
            if (value != _strokeGapLength)
            {
                _strokeGapLength = value;
                ClearCachedPaint();
            }
        }
    }

    #region Dropshadow

    SKColor _dropshadowColor;

    public SKColor DropshadowColor
    {
        get => _dropshadowColor;
        set
        {
            _dropshadowColor = value;
            ClearCachedPaint();
        }
    }

    public int DropshadowAlpha
    {
        get => DropshadowColor.Alpha;
        set
        {
            this.DropshadowColor = new SKColor(this.DropshadowColor.Red, this.DropshadowColor.Green, this.DropshadowColor.Blue, (byte)value);
        }
    }

    public int DropshadowBlue
    {
        get => DropshadowColor.Blue;
        set
        {
            this.DropshadowColor = new SKColor(this.DropshadowColor.Red, this.DropshadowColor.Green, (byte)value, this.DropshadowColor.Alpha);
        }
    }

    public int DropshadowGreen
    {
        get => DropshadowColor.Green;
        set
        {
            this.DropshadowColor = new SKColor(this.DropshadowColor.Red, (byte)value, this.DropshadowColor.Blue, this.DropshadowColor.Alpha);
        }
    }

    public int DropshadowRed
    {
        get => DropshadowColor.Red;
        set
        {
            this.DropshadowColor = new SKColor((byte)value, this.DropshadowColor.Green, this.DropshadowColor.Blue, this.DropshadowColor.Alpha);
        }
    }

    private bool _hasDropshadow;

    public bool HasDropshadow
    {
        get => _hasDropshadow;
        set
        {
            _hasDropshadow = value;
            ClearCachedPaint();
        }
    }

    private float _dropshadowOffsetX;

    public float DropshadowOffsetX
    {
        get => _dropshadowOffsetX;
        set
        {
            _dropshadowOffsetX = value;
            ClearCachedPaint();
        }
    }

    private float _dropshadowOffsetY;

    public float DropshadowOffsetY
    {
        get => _dropshadowOffsetY;
        set
        {
            _dropshadowOffsetY = value;
            ClearCachedPaint();
        }
    }

    private float _dropshadowBlurX;

    /// <summary>
    /// Horizontal visible blur radius of the dropshadow, in pixels. Passed to
    /// <c>SKImageFilter.CreateDropShadow</c> as a sigma of one third of this value.
    /// </summary>
    public float DropshadowBlurX
    {
        get => _dropshadowBlurX;
        set
        {
            _dropshadowBlurX = value;
            ClearCachedPaint();
        }
    }

    private float _dropshadowBlurY;

    /// <summary>
    /// Vertical visible blur radius of the dropshadow, in pixels. See <see cref="DropshadowBlurX"/>.
    /// </summary>
    public float DropshadowBlurY
    {
        get => _dropshadowBlurY;
        set
        {
            _dropshadowBlurY = value;
            ClearCachedPaint();
        }
    }

    #endregion


    public bool FlipHorizontal
    {
        get;
        set;
    }

    public bool FlipVertical
    {
        get;
        set;
    }

    public object? Tag { get; set; }

#endregion

    public RenderableShapeBase()
    {
        Width = 32;
        Height = 32; 
        Alpha = 255;
        Alpha1 = 255;
        Alpha2 = 255;
        this.Visible = true;
        mChildren = new ();

    }

    public bool IsRenderTarget => false;

    /// <summary>
    /// Optional callback invoked at the end of <see cref="PreRender"/>, wired by the owning
    /// GraphicalUiElement runtime (see <c>SkiaShapeRuntime.SetContainedShape</c>) so per-frame,
    /// GUE-level PreRender logic (e.g. the two-slot stroke Width/Height mirror, or
    /// <c>StrokeWidthUnits.ScreenPixel</c> resolution) still runs even when this renderable is a
    /// TOP-LEVEL <see cref="RenderingLibrary.Graphics.Layer"/> member added via
    /// <c>GraphicalUiElement.AddToManagers</c>. In that case only this raw renderable (the fill
    /// slot, <c>mContainedObjectAsIpso</c>) is registered on the Layer -- the GUE wrapper's own
    /// <c>PreRender()</c> override is otherwise unreachable, since the render walk only calls
    /// <c>.PreRender()</c> on whatever object is actually a Layer/Children member (issue #4367
    /// follow-up). Mirrors the XNALIKE <c>RenderableShapeBase</c> / <c>AposShapeRuntime</c>
    /// <c>OnPreRender</c> hook (see the gum-monogame-rendering skill).
    /// </summary>
    public Action? OnPreRender { get; set; }

    public void PreRender() => OnPreRender?.Invoke();

    public void Render(ISystemManagers managers)
    {
#if SKIA
        var canvas = ((SystemManagers)managers).Canvas;

        // Issue #3671 (Dark Pro / Forest Glade ListBoxItem regression) -- a stroke-only renderable
        // (IsFilled == false, the two-slot fill+stroke composition's dedicated stroke object) with
        // StrokeWidth <= 0 must not draw at all. SkiaSharp's SKPaint.StrokeWidth = 0 means "hairline"
        // (always a visible 1-device-pixel line), not "no stroke" -- unlike Apos.Shapes/raylib, which
        // treat 0 as invisible. Skipping the draw here is what SkiaShapeRuntime.IsFilled's own doc
        // comment already claims ("Stroke visibility is gated separately by StrokeWidth (0 hides
        // stroke)"), so this makes the code match the documented contract.
        // The stroke slot of a fill slot that draws it into its own layer (#6246) has nothing left to draw.
        if (IsPairedStroke && Parent is RenderableShapeBase fill && fill.PairedStrokeInLayer == this)
        {
            return;
        }

        ReplacesPaintBelow = false;

        if (CanRender)
        {
            var absoluteX = this.GetAbsoluteX();
            var absoluteY = this.GetAbsoluteY();
            var boundingRect = new SKRect(absoluteX, absoluteY, absoluteX + this.Width, absoluteY + this.Height);

            var rotation = this.GetAbsoluteRotation();
            var applyRotation = rotation != 0;
            if (applyRotation)
            {
                var oldX = boundingRect.Left;
                var oldY = boundingRect.Top;

                canvas.Save();

                boundingRect.Left = 0;
                boundingRect.Right -= oldX;
                boundingRect.Top = 0;
                boundingRect.Bottom -= oldY;

                canvas.Translate(oldX, oldY);
                canvas.RotateDegrees(-rotation);
            }

            SKRect blendBounds = boundingRect;
            boundingRect = GetStrokeAdjustedBounds(boundingRect);

            if (UsesBoundsLayer)
            {
                // See BlendsWholeBounds: draw the shape normally into a layer clipped to the bounds,
                // then composite that layer with the blend. A fill slot draws its stroke slot into the
                // same layer, so the pair blends as one shape (#6246).
                using SKPaint layerPaint = new SKPaint();
                layerPaint.ApplyBlend(Blend!.Value);
                canvas.Save();
                canvas.ClipRect(blendBounds);
                canvas.SaveLayer(layerPaint);
                DrawBound(boundingRect, canvas, this.GetAbsoluteRotation());
                RenderableShapeBase? pairedStroke = PairedStrokeInLayer;
                // A fully transparent ring draws nothing (replacing the fill with it would erase the ring zone).
                if (pairedStroke != null && !pairedStroke.IsTransparentStroke)
                {
                    pairedStroke.ReplacesPaintBelow = true;
                    pairedStroke.DrawBound(pairedStroke.GetStrokeAdjustedBounds(blendBounds), canvas, pairedStroke.GetAbsoluteRotation());
                }
                canvas.Restore();
                canvas.Restore();
            }
            else
            {
                DrawBound(boundingRect, canvas, this.GetAbsoluteRotation());
            }

            if (applyRotation)
            {
                canvas.Restore();
            }
        }
#endif
    }

    private bool CanRender =>
        Visible &&
            ((Width > 0 && Height > 0) || CanRenderAt0Dimension) &&
            (IsFilled || StrokeWidth > 0);

    // If this is stroke-only, then the stroke is centered around the bounds we pass in, so the bounds
    // move "in" by half of the stroke width.
    private bool IsTransparentStroke => Color.Alpha == 0 && !UseGradient && !HasDropshadow;

    private bool _replacesPaintBelow;

    // Set by the fill slot while it draws this stroke slot into its blend layer: the ring replaces the fill under
    // it, as one shape with a ring where it is stroked, instead of compositing over it (#6246). The fill slot overlaps
    // the ring by a pixel to hide the antialiasing seam, so a translucent ring would otherwise show the fill there.
    private bool ReplacesPaintBelow
    {
        get => _replacesPaintBelow;
        set
        {
            if (_replacesPaintBelow != value)
            {
                _replacesPaintBelow = value;
                ClearCachedPaint();
            }
        }
    }

    private float StrokeBoundsInset => IsFilled == false && IsOffsetAppliedForStroke ? StrokeWidth / 2.0f : 0f;

    private SKRect GetStrokeAdjustedBounds(SKRect bounds)
    {
        float inset = StrokeBoundsInset;
        bounds.Left += inset;
        bounds.Top += inset;
        bounds.Right -= inset;
        bounds.Bottom -= inset;
        return bounds;
    }

    /// <summary>
    /// True for the stroke slot of a Rectangle or Circle runtime, the child of that runtime's fill slot (set by
    /// <c>SkiaShapeRuntime.SetStrokeRenderable</c>). The two slots are one shape, so a whole-bounds blend
    /// composites them as one layer (#6246).
    /// </summary>
    internal bool IsPairedStroke { get; set; }

    // The stroke slot this fill slot draws into its own blend layer: Children[0] is the stroke slot the runtime
    // adds at construction. Null when the blend does not use a layer or the stroke slot would not draw.
    private RenderableShapeBase? PairedStrokeInLayer =>
        UsesBoundsLayer && !IsPairedStroke
        && mChildren.Count > 0 && mChildren[0] is RenderableShapeBase { IsPairedStroke: true, CanRender: true } stroke
            ? stroke
            : null;

    public BlendState BlendState => BlendState.AlphaBlend;

    /// <summary>
    /// The Gum blend mode applied when constructing the SKPaint used to draw this renderable.
    /// When null, the SkiaSharp default (SrcOver / standard alpha blending) is used.
    /// <c>ReplaceAlpha</c> has no <see cref="SKBlendMode"/>, so it is applied as a custom
    /// <see cref="SKBlender"/>; see <see cref="BlendToSkBlendModeExtensions"/>.
    /// </summary>
    public Gum.RenderingLibrary.Blend? Blend { get; set; }

    /// <summary>
    /// Whether <c>ReplaceAlpha</c> and <c>MinAlpha</c> act on the shape's whole bounds, so pixels
    /// inside the bounds that the shape leaves transparent still write alpha 0. True for the shapes
    /// MonoGame's Apos.Shapes path masks the same way (#5689): circle, rounded rectangle and arc.
    /// Sprites and nine-slices draw their whole bounds already; a line or polygon changes only the
    /// pixels it draws.
    /// </summary>
    protected virtual bool BlendsWholeBounds => false;

    private bool UsesBoundsLayer =>
        BlendsWholeBounds
        && (Blend == Gum.RenderingLibrary.Blend.ReplaceAlpha || Blend == Gum.RenderingLibrary.Blend.MinAlpha);

    public bool ClipsChildren { get; set; }

    SKRect _lastBoundingRect;

    SKPaint? _cachedPaint;
    protected void ClearCachedPaint()
    {
        _cachedPaint?.Dispose();
        _cachedPaint = null;
        NeedsUpdate = true;
    }



    float _lastAbsoluteRotation;
    protected SKPaint GetCachedPaint(SKRect boundingRect, float absoluteRotation)
    {
        if(boundingRect != _lastBoundingRect || absoluteRotation != _lastAbsoluteRotation)
        {
            _lastBoundingRect = boundingRect;
            _lastAbsoluteRotation = absoluteRotation;
            ClearCachedPaint();
        }
        if(_cachedPaint == null)
        {
            _cachedPaint = GetPaint(boundingRect, absoluteRotation);
        }
        return _cachedPaint;
    }

    protected virtual SKPaint GetPaint(SKRect boundingRect, float absoluteRotation)
    {
        var effectiveColor = this.Color;
        if (IsDimmed)
        {
            const double dimmingMuliplier = .9;

            effectiveColor = new SKColor(
                (byte)(this.Color.Red * dimmingMuliplier),
                (byte)(this.Color.Green * dimmingMuliplier),
                (byte)(this.Color.Blue * dimmingMuliplier),
                this.Color.Alpha);
        }



        var paint = new SKPaint
        {
            Color = effectiveColor,
            Style = IsFilled ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
            StrokeWidth = StrokeWidth,
            IsAntialias = _isAntialiased
        };

        if (HasDropshadow)
        {
            paint.ImageFilter = SKImageFilter.CreateDropShadow(
                        DropshadowOffsetX,
                        // See https://stackoverflow.com/questions/60456526/how-can-i-tell-the-amount-of-space-needed-for-a-skia-dropshadow
                        DropshadowOffsetY,
                        DropshadowBlurX / 3.0f,
                        DropshadowBlurY / 3.0f,
                        DropshadowColor);
        }

        if (UseGradient)
        {
            ApplyGradientToPaint(boundingRect, paint, absoluteRotation);

            // Issue #2998 — gradient visibility comes from the stop alphas (Alpha1 / Alpha2), not
            // the solid fill color. SkiaSharp modulates a shader's output by SKPaint.Color.alpha,
            // so leaving the (transparent-by-default) solid fill alpha in place would suppress a
            // legitimate gradient. Force the paint opaque so the stops drive visibility — two
            // transparent stops still render nothing. Mirrors the Apos ShouldPaintGradient and
            // raylib ShouldPaintFillGradient gates, which also ignore the solid fill alpha.
            paint.Color = new SKColor(paint.Color.Red, paint.Color.Green, paint.Color.Blue, 255);
        }

        if (!IsFilled && StrokeDashLength > 0 && StrokeGapLength > 0)
        {
            paint.PathEffect = SKPathEffect.CreateDash(
                new[] { StrokeDashLength, StrokeGapLength }, phase: 0);
        }

        if (_replacesPaintBelow)
        {
            paint.BlendMode = SKBlendMode.Src;
        }

        // The whole-bounds blends are applied by Render, to a layer; see BlendsWholeBounds.
        if (Blend.HasValue && !UsesBoundsLayer)
        {
            paint.ApplyBlend(Blend.Value);
        }

        // If you add new paint properties here, don't forget to also add them to
        // FlatRedBall's RenderableSkiaObject.CreatePaint() in Engines/SkiaGum/Renderables/RenderableSkiaObject.cs
        return paint;
    }

    public virtual void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {

    }

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        mParent = parent;
    }

    #region IVisible Implementation

    /// <inheritdoc/>
    public bool Visible
    {
        get;
        set;
    }

    /// <inheritdoc/>
    public bool AbsoluteVisible => ((IVisible)this).GetAbsoluteVisible();
    /// <inheritdoc/>
    IVisible? IVisible.Parent => ((IRenderableIpso)this).Parent as IVisible;

    public string BatchKey => string.Empty;
    public object? BatchSortKey => null;

    #endregion

    protected void ApplyGradientToPaint(SKRect boundingRect, SKPaint paint, float absoluteRotation)
    {
        var firstColor = new SKColor((byte)Red1, (byte)Green1, (byte)Blue1, (byte)Alpha1);
        var secondColor = new SKColor((byte)Red2, (byte)Green2, (byte)Blue2, (byte)Alpha2);

        var effectiveGradientX1 = GradientX1;
        switch (this.GradientX1Units)
        {
            case GeneralUnitType.PixelsFromMiddle:
                effectiveGradientX1 += Width / 2.0f;
                break;
            case GeneralUnitType.PixelsFromLarge:
                effectiveGradientX1 += Width;
                break;
            case GeneralUnitType.Percentage:
                effectiveGradientX1 = Width * GradientX1 / 100;
                break;
        }

        var effectiveGradientX2 = GradientX2;
        switch (this.GradientX2Units)
        {
            case GeneralUnitType.PixelsFromMiddle:
                effectiveGradientX2 += Width / 2.0f;
                break;
            case GeneralUnitType.PixelsFromLarge:
                effectiveGradientX2 += Width;
                break;
            case GeneralUnitType.Percentage:
                effectiveGradientX2 = Width * GradientX2 / 100;
                break;
        }

        var effectiveGradientY1 = GradientY1;
        switch (this.GradientY1Units)
        {
            case GeneralUnitType.PixelsFromMiddle:
                effectiveGradientY1 += Height / 2.0f;
                break;
            case GeneralUnitType.PixelsFromLarge:
                effectiveGradientY1 += Height;
                break;
            case GeneralUnitType.Percentage:
                effectiveGradientY1 = Height * GradientY1 / 100;
                break;
        }

        var effectiveGradientY2 = GradientY2;
        switch (this.GradientY2Units)
        {
            case GeneralUnitType.PixelsFromMiddle:
                effectiveGradientY2 += Height / 2.0f;
                break;
            case GeneralUnitType.PixelsFromLarge:
                effectiveGradientY2 += Height;
                break;
            case GeneralUnitType.Percentage:
                effectiveGradientY2 = Height * GradientY2 / 100;
                break;
        }

        var rectToUse = boundingRect;
        if (absoluteRotation != 0)
        {
            rectToUse = Unrotate(boundingRect, absoluteRotation);
        }
        else
        {
            // If we apply rotation, then the camera coordinates are adjusted such that the gradient coordiantes are relative to the object.
            // Otherwise, they are not so we need to offset:
            // boundingRect is pulled in by half the stroke width for a stroke-only shape; the gradient is placed from the shape's box.
            float strokeInset = StrokeBoundsInset;
            effectiveGradientX1 += rectToUse.Left - strokeInset;
            effectiveGradientY1 += rectToUse.Top - strokeInset;
            effectiveGradientX2 += rectToUse.Left - strokeInset;
            effectiveGradientY2 += rectToUse.Top - strokeInset;
        }

        if (GradientType == GradientType.Linear)
        {

            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(effectiveGradientX1, effectiveGradientY1), // left, top
                new SKPoint(effectiveGradientX2, effectiveGradientY2), // right, bottom
                new SKColor[] { firstColor, secondColor },
                new float[] { 0, 1 },
                SKShaderTileMode.Clamp);
        }
        else if (GradientType == GradientType.Radial)
        {
            var effectiveOuterRadius = GradientOuterRadius;

            switch (GradientOuterRadiusUnits)
            {
                case Gum.DataTypes.DimensionUnitType.PercentageOfParent:
                    effectiveOuterRadius = Width * GradientOuterRadius / 100;
                    break;
                case Gum.DataTypes.DimensionUnitType.RelativeToParent:
                    effectiveOuterRadius = Width + GradientOuterRadius;
                    break;
            }

            if (effectiveOuterRadius <= 0)
            {
                effectiveOuterRadius = 100;
            }

            var effectiveInnerRadius = GradientInnerRadius;

            switch (GradientInnerRadiusUnits)
            {
#pragma warning disable CS0618 // obsolete types and members still load from older projects
                case Gum.DataTypes.DimensionUnitType.Percentage:
                    effectiveInnerRadius = Width * GradientInnerRadius / 100;
                    break;
                case Gum.DataTypes.DimensionUnitType.RelativeToContainer:
#pragma warning restore CS0618
                    effectiveInnerRadius = Width + GradientInnerRadius;
                    break;
            }

            var innerToOuterRatio = effectiveInnerRadius / effectiveOuterRadius;


            paint.Shader = SKShader.CreateRadialGradient(
                new SKPoint(effectiveGradientX1, effectiveGradientY1), // center
                effectiveOuterRadius,
                new SKColor[] { firstColor, secondColor },
                new float[] { innerToOuterRatio, 1 },
                SKShaderTileMode.Clamp);
        }
    }

    SKRect Unrotate(SKRect beforeRotation, float angleToUndoDegrees)
    {
        var pointBefore = new Vector2(beforeRotation.Left, beforeRotation.Top);

        var middleOffset = new Vector2(beforeRotation.Width / 2.0f, beforeRotation.Height / 2.0f);
        MathFunctions.RotateVector(ref middleOffset, MathHelper.ToRadians(-angleToUndoDegrees));

        var middle = pointBefore + middleOffset;

        // convert to a "positive-Y-is-up" system:
        pointBefore.Y *= -1;
        middle.Y *= -1;
        MathFunctions.RotatePointAroundPoint(middle, ref pointBefore, MathHelper.ToRadians(-angleToUndoDegrees));
        pointBefore.Y *= -1;

        return new SKRect(pointBefore.X, pointBefore.Y, pointBefore.X + beforeRotation.Width, pointBefore.Y + beforeRotation.Height);
    }

    /// <summary>
    /// Releases the cached paint and disposes disposable children. A derived renderable that owns
    /// native resources (such as an image it built from its texture) overrides this to release them.
    /// </summary>
    public virtual void Dispose()
    {
        ClearCachedPaint();

        if(this.Children != null)
        {
            foreach(var child in this.Children)
            {
                if (child is IDisposable disposableChild)
                {
                    disposableChild.Dispose();
                }
            }
        }
    }

    public void StartBatch(ISystemManagers systemManagers)
    {
    }

    public void EndBatch(ISystemManagers systemManagers)
    {
    }
}
