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

namespace Gum.Themes.Template.Variants;

/// <summary>
/// "Rich" variant of the Template slider thumb. Compared to the flat
/// <see cref="Gum.Themes.Template.SliderThumbVisual"/> this changes ONLY the body
/// shape: the circle is built with <see cref="TemplateShapes.FilledCircleWithDropshadow"/>
/// for a soft Gaussian "lift" (mirrors the Bubblegum slider thumb shadow) instead
/// of the flat <see cref="TemplateShapes.FilledCircle"/>. The shadow is dropped on
/// the disabled states. The palette tokens, the "ButtonCategory" wiring, and the
/// states are identical to the flat source.
/// <para>
/// Part of the opt-in Variants gallery - NOT registered by default; instantiated
/// by the Variants <see cref="SliderVisual"/>.
/// </para>
/// </summary>
public class SliderThumbVisual : InteractiveGue
{
    private const float Size = 16f;
    private const float FocusRingInset = 2f;
    private const float BorderThickness = 1f;

    // Soft Gaussian shadow under the thumb (translucent accent + blur > 0).
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

        _focusRing = TemplateShapes.CircleFocusRing(TemplateStyling.ActiveStyle.Colors.Accent, FocusRingInset, BorderThickness);
        AddChild(_focusRing);

        _body = TemplateShapes.FilledCircleWithDropshadow(
            color: TemplateStyling.ActiveStyle.Colors.Accent,
            shadowColor: TemplateStyling.ActiveStyle.Colors.AccentPressedGlow,
            offsetX: 0f,
            offsetY: ShadowOffsetY,
            blur: ShadowBlur,
            name: "ThumbBody");
        AddChild(_body);

        WireStates();
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => Apply(body: TemplateStyling.ActiveStyle.Colors.Accent, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => Apply(body: TemplateStyling.ActiveStyle.Colors.AccentHover, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => Apply(body: TemplateStyling.ActiveStyle.Colors.AccentPressed, ring: false, showShadow: true));

        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => Apply(body: TemplateStyling.ActiveStyle.Colors.Accent, ring: true, showShadow: true));

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => Apply(body: TemplateStyling.ActiveStyle.Colors.AccentHover, ring: true, showShadow: true));

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => Apply(body: TemplateStyling.ActiveStyle.Colors.DisabledAccent, ring: false, showShadow: false));

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => Apply(body: TemplateStyling.ActiveStyle.Colors.DisabledAccent, ring: true, showShadow: false));
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
