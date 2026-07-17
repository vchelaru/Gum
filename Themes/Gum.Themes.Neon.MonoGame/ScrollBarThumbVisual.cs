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

namespace Gum.Themes.Neon;

/// <summary>
/// Neon scroll-bar thumb. A rounded-rect body in <c>--acc</c> pink (matches
/// <c>.bb-sb-thm</c>) that brightens on hover and darkens on press. Wrapped by
/// RangeBase in a <see cref="Button"/>; the Neon Button template never gets
/// to apply because V3 ScrollBar instantiates the thumb directly — this visual
/// stands in.
/// </summary>
public class ScrollBarThumbVisual : InteractiveGue
{
    private const float CornerRadius = 1f;

    private readonly RectangleRuntime _body;

    private StateSaveCategory _buttonCategory = null!;

    public ScrollBarThumbVisual() : base(new InvisibleRenderable())
    {
        HasEvents = true;
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
        body.Name = "NeonScrollThumbBody";
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
        body.FillColor = NeonStyling.ActiveStyle.Colors.ScrollThumb;
        body.StrokeWidth = 0;
        return body;
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        // Muted steel-blue at rest. Hover/push step toward cyan but stay a
        // shade below full Accent so the scroll bar reads as secondary chrome.
        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => _body.FillColor = NeonStyling.ActiveStyle.Colors.ScrollThumb);

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => _body.FillColor = NeonStyling.ActiveStyle.Colors.ScrollThumbHover);

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => _body.FillColor = NeonStyling.ActiveStyle.Colors.ScrollThumbHover);

        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => _body.FillColor = NeonStyling.ActiveStyle.Colors.ScrollThumb);

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => _body.FillColor = NeonStyling.ActiveStyle.Colors.ScrollThumbHover);

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => _body.FillColor = NeonStyling.ActiveStyle.Colors.Disabled);

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => _body.FillColor = NeonStyling.ActiveStyle.Colors.Disabled);
    }

    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }
}
