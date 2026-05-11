using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum slider thumb. 20 px white circle with a 2 px Accent border and a
/// subtle pink-tinted shadow underneath, matching <c>.bb-sldr-thumb</c>.
/// Implemented as an <see cref="InteractiveGue"/> with a <c>ButtonCategoryState</c>
/// so RangeBase wraps it in a <see cref="Button"/> like the V3 default thumb.
/// </summary>
public class SliderThumbVisual : InteractiveGue
{
    private const float Size = 20f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 3f;
    private const float FocusRingThickness = 4f;

    /// <summary>
    /// Native Apos.Shapes Gaussian drop shadow — matches the CSS
    /// <c>box-shadow:0 2px 8px rgba(255,107,157,.4)</c>. Toggled per state via
    /// <c>_body.HasDropshadow</c>.
    /// </summary>
    private const float ShadowOffsetY = 2f;
    private const float ShadowBlur = 8f;
    private static readonly Color ShadowColor = new Color(255, 107, 157, 102);

    private readonly ColoredCircleRuntime _focusRing;
    private readonly ColoredCircleRuntime _body;
    private readonly ColoredCircleRuntime _border;

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

    private static ColoredCircleRuntime CreateBody()
    {
        ColoredCircleRuntime body = new ColoredCircleRuntime();
        body.Name = "BubblegumSliderThumbBody";
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
        body.Color = Color.White;
        // Native Gaussian drop shadow under the thumb — replaces the prior
        // single-circle approximation. Toggled per state via WireStates.
        body.HasDropshadow = true;
        body.DropshadowColor = ShadowColor;
        body.DropshadowOffsetX = 0f;
        body.DropshadowOffsetY = ShadowOffsetY;
        body.DropshadowBlurX = ShadowBlur;
        body.DropshadowBlurY = ShadowBlur;
        return body;
    }

    private static ColoredCircleRuntime CreateBorder()
    {
        ColoredCircleRuntime border = new ColoredCircleRuntime();
        border.Name = "BubblegumSliderThumbBorder";
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
        border.Color = BubblegumColors.Accent;
        return border;
    }

    private static ColoredCircleRuntime CreateFocusRing()
    {
        ColoredCircleRuntime ring = new ColoredCircleRuntime();
        ring.Name = "BubblegumSliderThumbFocusRing";
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
        ring.Color = BubblegumPalette.FocusRing;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => Apply(body: Color.White, border: BubblegumColors.Accent, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => Apply(body: Color.White, border: BubblegumColors.Accent, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => Apply(body: BubblegumColors.AccentLight, border: BubblegumColors.AccentDark, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => Apply(body: Color.White, border: BubblegumColors.Accent, ring: true, showShadow: true));

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => Apply(body: Color.White, border: BubblegumColors.Accent, ring: true, showShadow: true));

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => Apply(body: BubblegumColors.DisabledFill, border: BubblegumColors.Disabled, ring: false, showShadow: false));

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => Apply(body: BubblegumColors.DisabledFill, border: BubblegumColors.Disabled, ring: true, showShadow: false));
    }

    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }

    private void Apply(Color body, Color border, bool ring, bool showShadow)
    {
        _body.Color = body;
        _body.HasDropshadow = showShadow;
        _border.Color = border;
        _focusRing.Visible = ring;
    }
}
