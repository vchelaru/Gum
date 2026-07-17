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

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade scroll-bar thumb. A leaf-small rounded rect with a
/// translucent leaf-bright fill (matches CSS <c>.fg-sb-thm</c>). Brightens
/// on hover/press, stays a step below full Accent so the scroll bar reads
/// as secondary chrome.
/// </summary>
public class ScrollBarThumbVisual : InteractiveGue
{
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
        body.Name = "ForestGladeScrollThumbBody";
        body.XUnits = GeneralUnitType.PixelsFromMiddle;
        body.YUnits = GeneralUnitType.PixelsFromMiddle;
        body.XOrigin = HorizontalAlignment.Center;
        body.YOrigin = VerticalAlignment.Center;
        body.Width = 0;
        body.Height = 0;
        body.WidthUnits = DimensionUnitType.RelativeToParent;
        body.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplySmall(body);
        body.IsFilled = true;
        body.StrokeWidth = 0;
        // CSS .fg-sb-thm linear-gradient(180deg, rgba(71,246,65,.55), rgba(0,140,46,.55))
        // — vertical, leaf-bright at top fading to canopy-lit at the bottom.
        body.UseGradient = true;
        body.GradientType = GradientType.Linear;
        body.GradientX1Units = GeneralUnitType.PixelsFromMiddle;
        body.GradientY1Units = GeneralUnitType.PixelsFromSmall;
        body.GradientX1 = 0f;
        body.GradientY1 = 0f;
        body.GradientX2Units = GeneralUnitType.PixelsFromMiddle;
        body.GradientY2Units = GeneralUnitType.PixelsFromLarge;
        body.GradientX2 = 0f;
        body.GradientY2 = 0f;
        body.FillColor = new Color(71, 246, 65, 200);
        body.Color2 = new Color(0, 140, 46, 200);
        return body;
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Color restTop = new Color(71, 246, 65, 200);
        Color restBottom = new Color(0, 140, 46, 200);
        Color hoverTop = new Color(108, 255, 100, 230);
        Color hoverBottom = new Color(8, 178, 59, 230);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => ApplyGradient(restTop, restBottom, gradient: true));

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => ApplyGradient(hoverTop, hoverBottom, gradient: true));

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => ApplyGradient(hoverTop, hoverBottom, gradient: true));

        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => ApplyGradient(restTop, restBottom, gradient: true));

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => ApplyGradient(hoverTop, hoverBottom, gradient: true));

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => ApplyGradient(ForestGladeStyling.ActiveStyle.Colors.Disabled, ForestGladeStyling.ActiveStyle.Colors.Disabled, gradient: false));

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => ApplyGradient(ForestGladeStyling.ActiveStyle.Colors.Disabled, ForestGladeStyling.ActiveStyle.Colors.Disabled, gradient: false));
    }

    private void ApplyGradient(Color top, Color bottom, bool gradient)
    {
        _body.UseGradient = gradient;
        _body.Color2 = bottom;
        _body.FillColor = top;
    }

    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }
}
