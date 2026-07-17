using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;

namespace Gum.Themes.Neon;

/// <summary>
/// Neon slider thumb. 20 px white circle with a 2 px Accent border and a
/// subtle pink-tinted shadow underneath, matching <c>.bb-sldr-thumb</c>.
/// Implemented as an <see cref="InteractiveGue"/> with a <c>ButtonCategoryState</c>
/// so RangeBase wraps it in a <see cref="Button"/> like the V3 default thumb.
/// </summary>
public class SliderThumbVisual : InteractiveGue
{
    private const float Size = 20f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 4f;
    private const float FocusRingThickness = 1f;

    /// <summary>
    /// Native Gaussian cyan glow under the thumb. CSS spec is
    /// <c>box-shadow:0 0 10px rgba(0,229,255,.5)</c>; bumped per the
    /// gum-theming skill's sRGB note so the halo reads as bright in-engine.
    /// </summary>
    private const float ShadowOffsetY = 0f;
    private const float ShadowBlur = 32f;

    private readonly CircleRuntime _focusRing;
    private readonly CircleRuntime _body;
    private readonly CircleRuntime _border;

    private StateSaveCategory _buttonCategory = null!;

    public SliderThumbVisual() : base(new InvisibleRenderable())
    {
        HasEvents = true;
        Width = Size;
        Height = Size;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _body = CreateBody();
        AddChild(_body);

        _border = CreateBorder();
        AddChild(_border);

        WireStates();
    }

    private static CircleRuntime CreateBody()
    {
        CircleRuntime body = new CircleRuntime();
        body.Name = "NeonSliderThumbBody";
        body.X = 0;
        body.Y = 0;
        body.XUnits = GeneralUnitType.PixelsFromMiddle;
        body.YUnits = GeneralUnitType.PixelsFromMiddle;
        body.XOrigin = HorizontalAlignment.Center;
        body.YOrigin = VerticalAlignment.Center;
        body.Width = 0;
        body.Height = 0;
        body.WidthUnits = DimensionUnitType.RelativeToParent;
        body.HeightUnits = DimensionUnitType.RelativeToParent;
        body.IsFilled = true;
        body.FillColor = NeonStyling.ActiveStyle.Colors.Surface1;
        body.StrokeWidth = 0;
        // Native Gaussian drop shadow under the thumb — replaces the prior
        // single-circle approximation. Toggled per state via WireStates.
        body.HasDropshadow = true;
        body.DropshadowColor = NeonStyling.ActiveStyle.Colors.SliderThumbShadow;
        body.DropshadowOffsetX = 0f;
        body.DropshadowOffsetY = ShadowOffsetY;
        body.DropshadowBlur = ShadowBlur;
        return body;
    }

    private static CircleRuntime CreateBorder()
    {
        CircleRuntime border = new CircleRuntime();
        border.Name = "NeonSliderThumbBorder";
        border.X = 0;
        border.Y = 0;
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.StrokeColor = NeonStyling.ActiveStyle.Colors.Accent;
        return border;
    }

    private static CircleRuntime CreateFocusRing()
    {
        CircleRuntime ring = new CircleRuntime();
        ring.Name = "NeonSliderThumbFocusRing";
        ring.X = 0;
        ring.Y = 0;
        ring.XUnits = GeneralUnitType.PixelsFromMiddle;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Center;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = FocusRingInset * 2f;
        ring.Height = FocusRingInset * 2f;
        ring.WidthUnits = DimensionUnitType.RelativeToParent;
        ring.HeightUnits = DimensionUnitType.RelativeToParent;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.StrokeColor = NeonStyling.ActiveStyle.Colors.FocusRing;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => Apply(body: NeonStyling.ActiveStyle.Colors.Surface1, border: NeonStyling.ActiveStyle.Colors.Accent, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => Apply(body: NeonStyling.ActiveStyle.Colors.Surface1, border: NeonStyling.ActiveStyle.Colors.Accent, ring: false, showShadow: true));

        // Pushed kept solid (Surface1, not translucent) — translucent let the
        // half-filled track show through the thumb, which read as a half-empty
        // bubble rather than a pressed control.
        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => Apply(body: NeonStyling.ActiveStyle.Colors.Surface2, border: NeonStyling.ActiveStyle.Colors.Accent, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => Apply(body: NeonStyling.ActiveStyle.Colors.Surface1, border: NeonStyling.ActiveStyle.Colors.Accent, ring: true, showShadow: true));

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => Apply(body: NeonStyling.ActiveStyle.Colors.Surface1, border: NeonStyling.ActiveStyle.Colors.Accent, ring: true, showShadow: true));

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => Apply(body: NeonStyling.ActiveStyle.Colors.Disabled, border: NeonStyling.ActiveStyle.Colors.Disabled, ring: false, showShadow: false));

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => Apply(body: NeonStyling.ActiveStyle.Colors.Disabled, border: NeonStyling.ActiveStyle.Colors.Disabled, ring: true, showShadow: false));
    }

    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }

    private void Apply(Color body, Color border, bool ring, bool showShadow)
    {
        _body.FillColor = body;
        _body.HasDropshadow = showShadow;
        _border.StrokeColor = border;
        _focusRing.Visible = ring;
    }
}
