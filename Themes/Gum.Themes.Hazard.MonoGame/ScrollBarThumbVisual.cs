using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard scroll-bar thumb. An <see cref="InteractiveGue"/> with a rounded-rect
/// body sized so <c>RangeBase</c> can wrap it in a <see cref="Button"/> and drive
/// its visual states via the "ButtonCategory" state set. Mirrors the
/// <see cref="SliderThumbVisual"/> trick — the V3 ScrollBar instantiates
/// <c>new ButtonVisual()</c> directly for its thumb, so the Hazard Button
/// template never gets a chance to apply; the subclass swaps in this visual
/// instead. Uses a de-emphasized gray palette (Border / BorderHover / Muted)
/// so the scroll bar reads as navigation chrome rather than a primary control.
/// </summary>
public class ScrollBarThumbVisual : InteractiveGue
{
    private const float CornerRadius = 0f;

    private readonly RectangleRuntime _body;

    private StateSaveCategory _buttonCategory = null!;

    public ScrollBarThumbVisual() : base(new InvisibleRenderable())
    {
        HasEvents = true;
        // Size is driven by the parent ThumbContainer in the ScrollBar layout;
        // start at full container size and rely on the consumer to inset.
        Width = 0f;
        Height = 0f;
        WidthUnits = DimensionUnitType.RelativeToParent;
        HeightUnits = DimensionUnitType.RelativeToParent;

        _body = HazardShapes.Fill(HazardStyling.ActiveStyle.Colors.Border, CornerRadius, "HazardScrollThumbBody");
        AddChild(_body);

        WireStates();
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => _body.FillColor = HazardStyling.ActiveStyle.Colors.Border);

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => _body.FillColor = HazardStyling.ActiveStyle.Colors.BorderHover);

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => _body.FillColor = HazardStyling.ActiveStyle.Colors.Muted);

        // No focus ring on a scroll-bar thumb — keyboard scroll focus lives
        // on the scrollable container, not the thumb itself. Match the
        // Enabled / Highlighted look so a focused thumb still de-emphasizes.
        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => _body.FillColor = HazardStyling.ActiveStyle.Colors.Border);

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => _body.FillColor = HazardStyling.ActiveStyle.Colors.BorderHover);

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => _body.FillColor = HazardStyling.ActiveStyle.Colors.DisabledBorder);

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => _body.FillColor = HazardStyling.ActiveStyle.Colors.DisabledBorder);
    }

    // Duplicated from SliderThumbVisual rather than shared — each thumb visual
    // keeps its own copy so the template files stay self-contained.
    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }
}
