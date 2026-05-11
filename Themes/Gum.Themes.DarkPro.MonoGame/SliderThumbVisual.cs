using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro slider thumb. An <see cref="InteractiveGue"/> with a circle body and
/// a focus ring sized so RangeBase can wrap it in a <see cref="Button"/> and drive
/// its visual states via <c>"ButtonCategoryState"</c>. Not a Dark Pro
/// <see cref="ButtonVisual"/> subclass — that visual is built around rounded
/// rectangles with corner radius 2, and forcing it into a circle would need to
/// dig into private fields. A purpose-built visual is cleaner and lighter.
/// </summary>
public class SliderThumbVisual : InteractiveGue
{
    private const float Size = 16f;
    private const float FocusRingInset = 2f;
    private const float BorderThickness = 1f;

    private readonly ColoredCircleRuntime _focusRing;
    private readonly ColoredCircleRuntime _body;

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

    private static ColoredCircleRuntime CreateBody()
    {
        ColoredCircleRuntime body = new ColoredCircleRuntime();
        body.Name = "DarkProSliderThumbBody";
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
        body.Color = DarkProColors.Accent;
        return body;
    }

    private static ColoredCircleRuntime CreateFocusRing()
    {
        // Sized (Size + 2 * FocusRingInset) so the 1-px stroke sits FocusRingInset
        // pixels outside the body.
        ColoredCircleRuntime ring = new ColoredCircleRuntime();
        ring.Name = "DarkProSliderThumbFocusRing";
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
        ring.StrokeWidth = BorderThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.Color = DarkProColors.Accent;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => Apply(body: DarkProColors.Accent, ring: false));

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => Apply(body: DarkProColors.HoverAccent, ring: false));

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => Apply(body: DarkProColors.AccentPressed, ring: false));

        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => Apply(body: DarkProColors.Accent, ring: true));

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => Apply(body: DarkProColors.HoverAccent, ring: true));

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => Apply(body: DarkProColors.DisabledThumb, ring: false));

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => Apply(body: DarkProColors.DisabledThumb, ring: true));
    }

    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }

    private void Apply(Color body, bool ring)
    {
        _body.Color = body;
        _focusRing.Visible = ring;
    }
}
