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
        body.FillColor = DarkProStyling.ActiveStyle.Colors.Accent;
        body.StrokeWidth = 0;
        return body;
    }

    private static CircleRuntime CreateFocusRing()
    {
        // Sized (Size + 2 * FocusRingInset) so the 1-px stroke sits FocusRingInset
        // pixels outside the body.
        CircleRuntime ring = new CircleRuntime();
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
        ring.StrokeColor = DarkProStyling.ActiveStyle.Colors.Accent;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => Apply(body: DarkProStyling.ActiveStyle.Colors.Accent, ring: false));

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => Apply(body: DarkProStyling.ActiveStyle.Colors.HoverAccent, ring: false));

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => Apply(body: DarkProStyling.ActiveStyle.Colors.AccentPressed, ring: false));

        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => Apply(body: DarkProStyling.ActiveStyle.Colors.Accent, ring: true));

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => Apply(body: DarkProStyling.ActiveStyle.Colors.HoverAccent, ring: true));

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => Apply(body: DarkProStyling.ActiveStyle.Colors.DisabledThumb, ring: false));

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => Apply(body: DarkProStyling.ActiveStyle.Colors.DisabledThumb, ring: true));
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
