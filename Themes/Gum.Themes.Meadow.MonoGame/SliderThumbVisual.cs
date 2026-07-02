using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow slider thumb. A borderless 22 px white circle with a soft warm-brown
/// drop shadow (matches <c>.pp-sldr-thumb</c>), gaining a sky-blue focus ring
/// when focused. Implemented as an <see cref="InteractiveGue"/> with a
/// <c>ButtonCategoryState</c> so RangeBase wraps it in a <see cref="Button"/>
/// like the V3 default thumb.
/// </summary>
public class SliderThumbVisual : InteractiveGue
{
    private const float Size = 22f;
    private const float FocusRingInset = 3f;
    private const float FocusRingThickness = 3f;

    /// <summary>Soft warm-brown shadow under the thumb (CSS
    /// <c>0 2px 5px rgba(160,110,70,.35)</c>, alpha bumped per the theming skill).</summary>
    private const float ShadowOffsetY = 2f;
    private const float ShadowBlur = 8f;

    private readonly CircleRuntime _focusRing;
    private readonly CircleRuntime _body;

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

        WireStates();
    }

    private static CircleRuntime CreateBody()
    {
        CircleRuntime body = new CircleRuntime();
        body.Name = "MeadowSliderThumbBody";
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
        body.FillColor = MeadowStyling.ActiveStyle.Colors.White;
        body.StrokeWidth = 0;
        body.HasDropshadow = true;
        body.DropshadowColor = MeadowStyling.ActiveStyle.Colors.ThumbShadow;
        body.DropshadowOffsetX = 0f;
        body.DropshadowOffsetY = ShadowOffsetY;
        body.DropshadowBlur = ShadowBlur;
        return body;
    }

    private static CircleRuntime CreateFocusRing()
    {
        CircleRuntime ring = new CircleRuntime();
        ring.Name = "MeadowSliderThumbFocusRing";
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
        ring.StrokeColor = MeadowStyling.ActiveStyle.Colors.Blue;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => Apply(body: MeadowStyling.ActiveStyle.Colors.White, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => Apply(body: MeadowStyling.ActiveStyle.Colors.White, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => Apply(body: MeadowStyling.ActiveStyle.Colors.Cream2, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => Apply(body: MeadowStyling.ActiveStyle.Colors.White, ring: true, showShadow: true));

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => Apply(body: MeadowStyling.ActiveStyle.Colors.White, ring: true, showShadow: true));

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => Apply(body: MeadowStyling.ActiveStyle.Colors.Cream2, ring: false, showShadow: false));

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => Apply(body: MeadowStyling.ActiveStyle.Colors.Cream2, ring: true, showShadow: false));
    }

    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }

    private void Apply(Color body, bool ring, bool showShadow)
    {
        _body.FillColor = body;
        _body.HasDropshadow = showShadow;
        _focusRing.Visible = ring;
    }
}
