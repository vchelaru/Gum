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
/// Dark Pro scroll-bar thumb. An <see cref="InteractiveGue"/> with a rounded-rect
/// body sized so <c>RangeBase</c> can wrap it in a <see cref="Button"/> and drive
/// its visual states via <c>"ButtonCategoryState"</c>. Mirrors the
/// <see cref="SliderThumbVisual"/> trick — the V3 ScrollBar instantiates
/// <c>new ButtonVisual()</c> directly for its thumb, so the Dark Pro Button
/// template never gets a chance to apply; the subclass swaps in this visual
/// instead. Uses a de-emphasized gray palette (Border / BorderHover / Muted)
/// so the scroll bar reads as navigation chrome rather than a primary control.
/// </summary>
public class ScrollBarThumbVisual : InteractiveGue
{
    private const float CornerRadius = 2f;

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

        _body = CreateBody();
        AddChild(_body);

        WireStates();
    }

    private static RectangleRuntime CreateBody()
    {
        RectangleRuntime body = new RectangleRuntime();
        body.Name = "DarkProScrollThumbBody";
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
        body.CornerRadius = CornerRadius;
        body.IsFilled = true;
        body.FillColor = DarkProStyling.ActiveStyle.Colors.Border;
        body.StrokeWidth = 0;
        return body;
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => _body.FillColor = DarkProStyling.ActiveStyle.Colors.Border);

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => _body.FillColor = DarkProStyling.ActiveStyle.Colors.BorderHover);

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => _body.FillColor = DarkProStyling.ActiveStyle.Colors.Muted);

        // No focus ring on a scroll-bar thumb — keyboard scroll focus lives
        // on the scrollable container, not the thumb itself. Match the
        // Enabled / Highlighted look so a focused thumb still de-emphasizes.
        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => _body.FillColor = DarkProStyling.ActiveStyle.Colors.Border);

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => _body.FillColor = DarkProStyling.ActiveStyle.Colors.BorderHover);

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => _body.FillColor = DarkProStyling.ActiveStyle.Colors.DisabledBorder);

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => _body.FillColor = DarkProStyling.ActiveStyle.Colors.DisabledBorder);
    }

    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }
}
