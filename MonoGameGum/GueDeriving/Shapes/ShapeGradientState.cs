#if XNALIKE
using Gum.Converters;
using Gum.DataTypes;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.GueDeriving;

/// <summary>
/// Backing state and fill/stroke push logic for the Gradient property block shared by
/// <see cref="CircleRuntime"/> and <see cref="RectangleRuntime"/> (issues #2791 / #2818 — the
/// two runtimes' Gradient regions were near-byte-identical). A pure data struct: callers pass
/// the current fill/stroke slots (cast to <see cref="IGradientedRenderable"/>) on every call
/// rather than the struct caching them, so a stale reference can never survive a
/// <c>Clone()</c> rebuild of those slots. Being a struct (not a class) also means the owning
/// runtime's <c>MemberwiseClone</c>-based <c>Clone()</c> copies every backing field by value for
/// free, matching the zero-explicit-handling pattern the runtimes already rely on for their
/// other backing fields.
/// </summary>
struct ShapeGradientState
{
    public bool UseGradient;
    public GradientType GradientType;
    public int Alpha2;
    public int Red2;
    public int Green2;
    public int Blue2;
    public float GradientX1;
    public GeneralUnitType GradientX1Units;
    public float GradientY1;
    public GeneralUnitType GradientY1Units;
    public float GradientX2;
    public GeneralUnitType GradientX2Units;
    public float GradientY2;
    public GeneralUnitType GradientY2Units;
    public float GradientInnerRadius;
    public DimensionUnitType GradientInnerRadiusUnits;
    public float GradientOuterRadius;
    public DimensionUnitType GradientOuterRadiusUnits;

    /// <summary>Composed from <see cref="Red2"/>/<see cref="Green2"/>/<see cref="Blue2"/>/<see cref="Alpha2"/> — no dedicated backing field.</summary>
    public Color Color2 => new Color(Red2, Green2, Blue2, Alpha2);

    public void SetUseGradient(bool value, IGradientedRenderable? fill, IGradientedRenderable? stroke, bool isFilled)
    {
        UseGradient = value;
        PushGradientGate(fill, stroke, isFilled);
    }

    public void SetGradientType(GradientType value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientType = value;
        if (fill != null) fill.GradientType = value;
        if (stroke != null) stroke.GradientType = value;
    }

    public void SetAlpha2(int value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        Alpha2 = value;
        if (fill != null) fill.Alpha2 = value;
        if (stroke != null) stroke.Alpha2 = value;
    }

    public void SetRed2(int value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        Red2 = value;
        if (fill != null) fill.Red2 = value;
        if (stroke != null) stroke.Red2 = value;
    }

    public void SetGreen2(int value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        Green2 = value;
        if (fill != null) fill.Green2 = value;
        if (stroke != null) stroke.Green2 = value;
    }

    public void SetBlue2(int value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        Blue2 = value;
        if (fill != null) fill.Blue2 = value;
        if (stroke != null) stroke.Blue2 = value;
    }

    public void SetGradientX1(float value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientX1 = value;
        if (fill != null) fill.GradientX1 = value;
        if (stroke != null) stroke.GradientX1 = value;
    }

    public void SetGradientX1Units(GeneralUnitType value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientX1Units = value;
        if (fill != null) fill.GradientX1Units = value;
        if (stroke != null) stroke.GradientX1Units = value;
    }

    public void SetGradientY1(float value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientY1 = value;
        if (fill != null) fill.GradientY1 = value;
        if (stroke != null) stroke.GradientY1 = value;
    }

    public void SetGradientY1Units(GeneralUnitType value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientY1Units = value;
        if (fill != null) fill.GradientY1Units = value;
        if (stroke != null) stroke.GradientY1Units = value;
    }

    public void SetGradientX2(float value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientX2 = value;
        if (fill != null) fill.GradientX2 = value;
        if (stroke != null) stroke.GradientX2 = value;
    }

    public void SetGradientX2Units(GeneralUnitType value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientX2Units = value;
        if (fill != null) fill.GradientX2Units = value;
        if (stroke != null) stroke.GradientX2Units = value;
    }

    public void SetGradientY2(float value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientY2 = value;
        if (fill != null) fill.GradientY2 = value;
        if (stroke != null) stroke.GradientY2 = value;
    }

    public void SetGradientY2Units(GeneralUnitType value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientY2Units = value;
        if (fill != null) fill.GradientY2Units = value;
        if (stroke != null) stroke.GradientY2Units = value;
    }

    public void SetGradientInnerRadius(float value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientInnerRadius = value;
        if (fill != null) fill.GradientInnerRadius = value;
        if (stroke != null) stroke.GradientInnerRadius = value;
    }

    public void SetGradientInnerRadiusUnits(DimensionUnitType value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientInnerRadiusUnits = value;
        if (fill != null) fill.GradientInnerRadiusUnits = value;
        if (stroke != null) stroke.GradientInnerRadiusUnits = value;
    }

    public void SetGradientOuterRadius(float value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientOuterRadius = value;
        if (fill != null) fill.GradientOuterRadius = value;
        if (stroke != null) stroke.GradientOuterRadius = value;
    }

    public void SetGradientOuterRadiusUnits(DimensionUnitType value, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        GradientOuterRadiusUnits = value;
        if (fill != null) fill.GradientOuterRadiusUnits = value;
        if (stroke != null) stroke.GradientOuterRadiusUnits = value;
    }

    /// <summary>
    /// Routes the gradient gate to the ACTIVE body slot (fill when <paramref name="isFilled"/>,
    /// else stroke) and forces the inactive slot's gate off — a single gradient is the active
    /// body's paint, so the other slot renders solid rather than sharing the gradient and
    /// compositing invisibly over it. Called from both <see cref="SetUseGradient"/> and the
    /// owning runtime's <c>IsFilled</c> setter (toggling <c>IsFilled</c> re-routes the gate).
    /// </summary>
    public void PushGradientGate(IGradientedRenderable? fill, IGradientedRenderable? stroke, bool isFilled)
    {
        var active = isFilled ? fill : stroke;
        var inactive = isFilled ? stroke : fill;
        if (active != null) active.UseGradient = UseGradient;
        if (inactive != null) inactive.UseGradient = false;
    }

    /// <summary>
    /// Mirrors each slot's own solid body color into its Red1/Green1/Blue1/Alpha1 gradient
    /// start so the gradient begins at the color the shape was already showing (no jump when
    /// <c>UseGradient</c> toggles). Called from the owning runtime's <c>FillColor</c> /
    /// <c>StrokeColor</c> setters and constructor.
    /// </summary>
    public void PushGradientStart(Color fillColor, Color strokeColor, IGradientedRenderable? fill, IGradientedRenderable? stroke)
    {
        if (fill != null)
        {
            fill.Red1 = fillColor.R;
            fill.Green1 = fillColor.G;
            fill.Blue1 = fillColor.B;
            fill.Alpha1 = fillColor.A;
        }
        if (stroke != null)
        {
            stroke.Red1 = strokeColor.R;
            stroke.Green1 = strokeColor.G;
            stroke.Blue1 = strokeColor.B;
            stroke.Alpha1 = strokeColor.A;
        }
    }

    /// <summary>
    /// Pushes every backing field onto freshly-rebuilt fill/stroke slots. Used only from
    /// <c>Clone()</c> to close a pre-existing gap: the cloned runtime's gradient state
    /// previously never reached its new renderable slots (only the backing fields survived via
    /// <c>MemberwiseClone</c>), so a clone with <c>UseGradient = true</c> silently rendered
    /// without its gradient until some other property write happened to re-trigger it.
    /// </summary>
    public void PushAll(IGradientedRenderable? fill, IGradientedRenderable? stroke, bool isFilled, Color fillColor, Color strokeColor)
    {
        SetGradientType(GradientType, fill, stroke);
        SetAlpha2(Alpha2, fill, stroke);
        SetRed2(Red2, fill, stroke);
        SetGreen2(Green2, fill, stroke);
        SetBlue2(Blue2, fill, stroke);
        SetGradientX1(GradientX1, fill, stroke);
        SetGradientX1Units(GradientX1Units, fill, stroke);
        SetGradientY1(GradientY1, fill, stroke);
        SetGradientY1Units(GradientY1Units, fill, stroke);
        SetGradientX2(GradientX2, fill, stroke);
        SetGradientX2Units(GradientX2Units, fill, stroke);
        SetGradientY2(GradientY2, fill, stroke);
        SetGradientY2Units(GradientY2Units, fill, stroke);
        SetGradientInnerRadius(GradientInnerRadius, fill, stroke);
        SetGradientInnerRadiusUnits(GradientInnerRadiusUnits, fill, stroke);
        SetGradientOuterRadius(GradientOuterRadius, fill, stroke);
        SetGradientOuterRadiusUnits(GradientOuterRadiusUnits, fill, stroke);
        PushGradientStart(fillColor, strokeColor, fill, stroke);
        PushGradientGate(fill, stroke, isFilled);
    }
}
#endif
