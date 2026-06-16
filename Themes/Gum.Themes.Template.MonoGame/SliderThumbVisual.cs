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

namespace Gum.Themes.Template;

/// <summary>
/// Template slider thumb. An <see cref="InteractiveGue"/> with a circular body and a
/// focus ring, sized so RangeBase can wrap it in a <see cref="Button"/> and drive its
/// states via the "ButtonCategory" state set.
///
/// A slider/scrollbar thumb is built directly on <see cref="InteractiveGue"/> rather
/// than as a Button subclass: V3's Slider creates the thumb as <c>new Button()</c> and
/// looks it up by the name "ThumbInstance", so the theme supplies its own visual under
/// that name. The thumb must set <see cref="InteractiveGue.HasEvents"/> = true so the
/// drag is picked up (the one place a theme intentionally wants events on a child).
/// </summary>
public class SliderThumbVisual : InteractiveGue
{
    private const float Size = 16f;
    private const float FocusRingInset = 2f;
    private const float BorderThickness = 1f;

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

        _focusRing = TemplateShapes.CircleFocusRing(TemplatePalette.Accent, FocusRingInset, BorderThickness);
        AddChild(_focusRing);

        _body = TemplateShapes.FilledCircle(TemplatePalette.Accent, "ThumbBody");
        AddChild(_body);

        WireStates();
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => Apply(body: TemplatePalette.Accent, ring: false));

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => Apply(body: TemplatePalette.AccentHover, ring: false));

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => Apply(body: TemplatePalette.AccentPressed, ring: false));

        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => Apply(body: TemplatePalette.Accent, ring: true));

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => Apply(body: TemplatePalette.AccentHover, ring: true));

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => Apply(body: TemplatePalette.DisabledAccent, ring: false));

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => Apply(body: TemplatePalette.DisabledAccent, ring: true));
    }

    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }

    private void Apply(Color body, bool ring)
    {
        _body.FillColor = body;
        _focusRing.Visible = ring;
    }
}
