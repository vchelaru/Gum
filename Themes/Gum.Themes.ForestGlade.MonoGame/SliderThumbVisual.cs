using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade slider thumb — dewdrop. 18 px sun-pale circle with a
/// sun-pale ~50% alpha border and a leaf-bright Gaussian glow underneath
/// matching the CSS <c>.fg-sldr-thumb</c> box-shadow.
/// </summary>
public class SliderThumbVisual : InteractiveGue
{
    private const float Size = 18f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 4f;
    private const float FocusRingThickness = 3f;

    private const float ShadowBlur = 22f;
    private const float ShadowBlurHover = 30f;

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
        body.Name = "ForestGladeSliderThumbBody";
        body.XUnits = GeneralUnitType.PixelsFromMiddle;
        body.YUnits = GeneralUnitType.PixelsFromMiddle;
        body.XOrigin = HorizontalAlignment.Center;
        body.YOrigin = VerticalAlignment.Center;
        body.Width = 0;
        body.Height = 0;
        body.WidthUnits = DimensionUnitType.RelativeToParent;
        body.HeightUnits = DimensionUnitType.RelativeToParent;
        body.IsFilled = true;
        // CSS radial-gradient(circle at 35% 30%, sun-pale, leaf-bright 65%, #008c2e).
        // 2-stop approximation: sun-pale centre → canopy-lit edge. Offset of
        // 35%/30% is implemented by nudging the gradient origin off-centre
        // (PixelsFromMiddle with a small negative offset).
        body.UseGradient = true;
        body.GradientType = GradientType.Radial;
        body.Color1 = ForestGladeColors.SunPale;
        body.Color2 = new Color(0, 140, 46); // CSS #008c2e outer stop
        // 35% / 30% from top-left on an 18 px circle ≈ ~-2.7 / -3.6 from middle.
        body.GradientX1Units = GeneralUnitType.PixelsFromMiddle;
        body.GradientY1Units = GeneralUnitType.PixelsFromMiddle;
        body.GradientX1 = -3f;
        body.GradientY1 = -4f;
        body.GradientInnerRadius = 0f;
        body.GradientInnerRadiusUnits = DimensionUnitType.Absolute;
        body.GradientOuterRadius = 50f;
        body.GradientOuterRadiusUnits = DimensionUnitType.PercentageOfParent;
        body.HasDropshadow = true;
        body.DropshadowColor = ForestGladePalette.GlowMedium;
        body.DropshadowOffsetX = 0f;
        body.DropshadowOffsetY = 0f;
        body.DropshadowBlurX = ShadowBlur;
        body.DropshadowBlurY = ShadowBlur;
        return body;
    }

    private static ColoredCircleRuntime CreateBorder()
    {
        ColoredCircleRuntime border = new ColoredCircleRuntime();
        border.Name = "ForestGladeSliderThumbBorder";
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
        border.Color = new Color(232, 255, 117, 128); // sun-pale .5 alpha
        return border;
    }

    private static ColoredCircleRuntime CreateFocusRing()
    {
        ColoredCircleRuntime ring = new ColoredCircleRuntime();
        ring.Name = "ForestGladeSliderThumbFocusRing";
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
        ring.Color = ForestGladeColors.AccentHalo;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Color restCentre = ForestGladeColors.SunPale;
        Color restEdge = new Color(0, 140, 46);
        Color pushedCentre = ForestGladeColors.LeafBright;
        Color pushedEdge = new Color(0, 112, 40);
        Color disabledFlat = ForestGladePalette.SliderDisabled;

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => Apply(centre: restCentre, edge: restEdge, border: new Color(232, 255, 117, 128),
                gradient: true, ring: false, showShadow: true, blur: ShadowBlur));

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => Apply(centre: restCentre, edge: restEdge, border: ForestGladeColors.SunPale,
                gradient: true, ring: false, showShadow: true, blur: ShadowBlurHover));

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => Apply(centre: pushedCentre, edge: pushedEdge, border: ForestGladeColors.SunPale,
                gradient: true, ring: false, showShadow: true, blur: ShadowBlurHover));

        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => Apply(centre: restCentre, edge: restEdge, border: ForestGladeColors.SunPale,
                gradient: true, ring: true, showShadow: true, blur: ShadowBlur));

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => Apply(centre: restCentre, edge: restEdge, border: ForestGladeColors.SunPale,
                gradient: true, ring: true, showShadow: true, blur: ShadowBlurHover));

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => Apply(centre: disabledFlat, edge: disabledFlat, border: new Color(232, 255, 117, 26),
                gradient: false, ring: false, showShadow: false, blur: 0f));

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => Apply(centre: disabledFlat, edge: disabledFlat, border: new Color(232, 255, 117, 26),
                gradient: false, ring: true, showShadow: false, blur: 0f));
    }

    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }

    private void Apply(Color centre, Color edge, Color border, bool gradient, bool ring, bool showShadow, float blur)
    {
        _body.UseGradient = gradient;
        _body.Color1 = centre;
        _body.Color2 = edge;
        _body.Color = centre;
        _body.HasDropshadow = showShadow;
        _body.DropshadowBlurX = blur;
        _body.DropshadowBlurY = blur;
        _border.Color = border;
        _focusRing.Visible = ring;
    }
}
