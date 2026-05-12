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
/// Forest Glade scroll-bar thumb. A leaf-small rounded rect with a
/// translucent leaf-bright fill (matches CSS <c>.fg-sb-thm</c>). Brightens
/// on hover/press, stays a step below full Accent so the scroll bar reads
/// as secondary chrome.
/// </summary>
public class ScrollBarThumbVisual : InteractiveGue
{
    private readonly RoundedRectangleRuntime _body;

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

    private static RoundedRectangleRuntime CreateBody()
    {
        RoundedRectangleRuntime body = new RoundedRectangleRuntime();
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
        body.Color = ForestGladePalette.ScrollThumb;
        return body;
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => _body.Color = ForestGladePalette.ScrollThumb);

        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => _body.Color = ForestGladePalette.ScrollThumbHover);

        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => _body.Color = ForestGladePalette.ScrollThumbHover);

        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => _body.Color = ForestGladePalette.ScrollThumb);

        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => _body.Color = ForestGladePalette.ScrollThumbHover);

        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => _body.Color = ForestGladeColors.Disabled);

        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => _body.Color = ForestGladeColors.Disabled);
    }

    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }
}
