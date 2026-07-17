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
    /// Native Gaussian drop shadow. CSS spec is
    /// <c>box-shadow:0 2px 8px rgba(255,107,157,.4)</c>; per the gum-theming
    /// skill, the CSS-literal alpha (102) reads too faint in-engine. Bumped
    /// ~1.55× to alpha 160 — matches the Button shadow weight so the slider
    /// thumb reads as part of the same "lifted control" family.
    /// </summary>
    private const float ShadowOffsetY = 2f;
    private const float ShadowBlur = 10f;
    private static readonly Color ShadowColor = new Color(255, 107, 157, 160);

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

        _focusRing = BubblegumShapes.CircleFocusRing(
            color: BubblegumStyling.ActiveStyle.Colors.FocusRing,
            inset: FocusRingInset,
            thickness: FocusRingThickness,
            name: "BubblegumSliderThumbFocusRing");
        AddChild(_focusRing);

        _body = BubblegumShapes.FilledCircleWithDropshadow(
            color: new Color(255, 255, 255),
            shadowColor: ShadowColor,
            offsetX: 0f,
            offsetY: ShadowOffsetY,
            blur: ShadowBlur,
            name: "BubblegumSliderThumbBody");
        AddChild(_body);

        _border = BubblegumShapes.CircleBorder(
            color: BubblegumStyling.ActiveStyle.Colors.Accent,
            thickness: BorderThickness,
            name: "BubblegumSliderThumbBorder");
        AddChild(_border);

        WireStates();
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => Apply(body: new Color(255, 255, 255), border: BubblegumStyling.ActiveStyle.Colors.Accent, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => Apply(body: new Color(255, 255, 255), border: BubblegumStyling.ActiveStyle.Colors.Accent, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => Apply(body: BubblegumStyling.ActiveStyle.Colors.AccentLight, border: BubblegumStyling.ActiveStyle.Colors.AccentDark, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => Apply(body: new Color(255, 255, 255), border: BubblegumStyling.ActiveStyle.Colors.Accent, ring: true, showShadow: true));

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => Apply(body: new Color(255, 255, 255), border: BubblegumStyling.ActiveStyle.Colors.Accent, ring: true, showShadow: true));

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => Apply(body: BubblegumStyling.ActiveStyle.Colors.DisabledFill, border: BubblegumStyling.ActiveStyle.Colors.Disabled, ring: false, showShadow: false));

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => Apply(body: BubblegumStyling.ActiveStyle.Colors.DisabledFill, border: BubblegumStyling.ActiveStyle.Colors.Disabled, ring: true, showShadow: false));
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
