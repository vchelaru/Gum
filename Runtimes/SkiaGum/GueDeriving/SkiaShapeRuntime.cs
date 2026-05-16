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
    /// On Skia today the contained renderable has a single color slot + <c>IsFilled</c> toggle —
    /// setting <em>both</em> <see cref="FillColor"/> and <see cref="StrokeColor"/> non-null is
    /// not yet visually composed; the most recently set non-null wins. Full two-slot
    /// composition (matching the XNA-like backends) tracked in issue #2790.
    /// </remarks>
    public SKColor? FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            if (value is SKColor fill)
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
        }
    }

    SKColor? _strokeColor;

    /// <summary>
    /// Color of the stroked outline. When set non-null, the contained renderable switches to
    /// stroke mode and renders with this color. When set null, the runtime falls back to fill
    /// (or, if <see cref="FillColor"/> is also null, alpha-0 / invisible).
    /// </summary>
    /// <remarks>
    /// On Skia today the contained renderable has a single color slot + <c>IsFilled</c> toggle —
    /// setting <em>both</em> <see cref="FillColor"/> and <see cref="StrokeColor"/> non-null is
    /// not yet visually composed; the most recently set non-null wins. Full two-slot
    /// composition (matching the XNA-like backends) tracked in issue #2790.
    /// </remarks>
    public SKColor? StrokeColor
    {
        get => _strokeColor;
        set
        {
            _strokeColor = value;
            if (value is SKColor stroke)
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
        }
    }
    #endregion

    #region Gradient Colors


    public int Blue1
    {
        get => ContainedRenderable.Blue1;
        set => ContainedRenderable.Blue1 = value;
    }

    public int Green1
    {
        get => ContainedRenderable.Green1;
        set => ContainedRenderable.Green1 = value;
    }

    public int Red1
    {
        get => ContainedRenderable.Red1;
        set => ContainedRenderable.Red1 = value;
    }

    public int Alpha1
    {
        get => ContainedRenderable.Alpha1;
        set => ContainedRenderable.Alpha1 = value;
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
        set => ContainedRenderable.Blue2 = value;
    }

    public int Green2
    {
        get => ContainedRenderable.Green2;
        set => ContainedRenderable.Green2 = value;
    }

    public int Red2
    {
        get => ContainedRenderable.Red2;
        set => ContainedRenderable.Red2 = value;
    }

    public int Alpha2
    {
        get => ContainedRenderable.Alpha2;
        set => ContainedRenderable.Alpha2 = value;
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
        set => ContainedRenderable.GradientX1 = value;
    }
    public GeneralUnitType GradientX1Units
    {
        get => ContainedRenderable.GradientX1Units;
        set => ContainedRenderable.GradientX1Units = value;
    }
    public float GradientY1
    {
        get => ContainedRenderable.GradientY1;
        set => ContainedRenderable.GradientY1 = value;
    }
    public GeneralUnitType GradientY1Units
    {
        get => ContainedRenderable.GradientY1Units;
        set => ContainedRenderable.GradientY1Units = value;
    }

    public float GradientX2
    {
        get => ContainedRenderable.GradientX2;
        set => ContainedRenderable.GradientX2 = value;
    }
    public GeneralUnitType GradientX2Units
    {
        get => ContainedRenderable.GradientX2Units;
        set => ContainedRenderable.GradientX2Units = value;
    }
    public float GradientY2
    {
        get => ContainedRenderable.GradientY2;
        set => ContainedRenderable.GradientY2 = value;
    }
    public GeneralUnitType GradientY2Units
    {
        get => ContainedRenderable.GradientY2Units;
        set => ContainedRenderable.GradientY2Units = value;
    }

    public bool UseGradient
    {
        get => ContainedRenderable.UseGradient;
        set => ContainedRenderable.UseGradient = value;
    }

    public GradientType GradientType
    {
        get => ContainedRenderable.GradientType;
        set => ContainedRenderable.GradientType = value;
    }

    public float GradientInnerRadius
    {
        get => ContainedRenderable.GradientInnerRadius;
        set => ContainedRenderable.GradientInnerRadius = value;
    }

    public DimensionUnitType GradientInnerRadiusUnits
    {
        get => ContainedRenderable.GradientInnerRadiusUnits;
        set => ContainedRenderable.GradientInnerRadiusUnits = value;
    }

    public float GradientOuterRadius
    {
        get => ContainedRenderable.GradientOuterRadius;
        set => ContainedRenderable.GradientOuterRadius = value;
    }

    public DimensionUnitType GradientOuterRadiusUnits
    {
        get => ContainedRenderable.GradientOuterRadiusUnits;
        set => ContainedRenderable.GradientOuterRadiusUnits = value;
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
    /// AA-capable so the value pushes straight through.
    /// </summary>
    public bool IsAntialiased
    {
        get => ContainedRenderable.IsAntialiased;
        set => ContainedRenderable.IsAntialiased = value;
    }

    public float StrokeDashLength
    {
        get => ContainedRenderable.StrokeDashLength;
        set => ContainedRenderable.StrokeDashLength = value;
    }

    public float StrokeGapLength
    {
        get => ContainedRenderable.StrokeGapLength;
        set => ContainedRenderable.StrokeGapLength = value;
    }

    #endregion

    #region Dropshadow

    public int DropshadowAlpha
    {
        get => ContainedRenderable.DropshadowAlpha;
        set => ContainedRenderable.DropshadowAlpha = value;
    }

    public int DropshadowBlue
    {
        get => ContainedRenderable.DropshadowBlue;
        set => ContainedRenderable.DropshadowBlue = value;
    }

    public int DropshadowGreen
    {
        get => ContainedRenderable.DropshadowGreen;
        set => ContainedRenderable.DropshadowGreen = value;
    }

    public int DropshadowRed
    {
        get => ContainedRenderable.DropshadowRed;
        set => ContainedRenderable.DropshadowRed = value;
    }


    public bool HasDropshadow
    {
        get => ContainedRenderable.HasDropshadow;
        set => ContainedRenderable.HasDropshadow = value;
    }

    public float DropshadowOffsetX
    {
        get => ContainedRenderable.DropshadowOffsetX;
        set => ContainedRenderable.DropshadowOffsetX = value;
    }
    public float DropshadowOffsetY
    {
        get => ContainedRenderable.DropshadowOffsetY;
        set => ContainedRenderable.DropshadowOffsetY = value;
    }

    public float DropshadowBlurX
    {
        get => ContainedRenderable.DropshadowBlurX;
        set => ContainedRenderable.DropshadowBlurX = value;
    }
    public float DropshadowBlurY
    {
        get => ContainedRenderable.DropshadowBlurY;
        set => ContainedRenderable.DropshadowBlurY = value;
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

        ContainedRenderable.StrokeWidth = strokeWidth;
        base.PreRender();
    }
}
