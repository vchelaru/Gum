using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using BaseTabItemVisual = Gum.Forms.DefaultVisuals.V3.TabItemVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade styled TabItem visual: a 2px-bordered rounded rectangle (CornerRadius=4) that is CanopyDeep when unselected and the button green when selected.
/// </summary>
public class TabItemVisual : BaseTabItemVisual
{
    internal const float CornerRadius = 4f;
    internal const float BorderThickness = 2f;

    private readonly RectangleRuntime _fill;

    public TabItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Replace the base NineSlice with the theme's fill and border; the text is reparented last so it draws on top.
        Background.Parent = null;
        TextInstance.Parent = null;

        _fill = CreateFill("ForestGladeTabFill", ForestGladeStyling.ActiveStyle.Colors.CanopyDeep);
        AddChild(_fill);
        AddChild(CreateBorder("ForestGladeTabBorder"));
        AddChild(TextInstance);

        WireStates();
        ApplyPalette(ForestGladeStyling.ActiveStyle.Colors.CanopyDeep, ForestGladeStyling.ActiveStyle.Colors.Text);
    }

    // Shared with TabControlVisual so the strip and the content panel use one set of shapes.
    internal static RectangleRuntime CreateFill(string name, Color color)
    {
        RectangleRuntime shape = NewShape(name);
        shape.IsFilled = true;
        shape.FillColor = color;
        shape.StrokeWidth = 0;
        return shape;
    }

    internal static RectangleRuntime CreateBorder(string name)
    {
        RectangleRuntime shape = NewShape(name);
        shape.IsFilled = false;
        shape.StrokeWidth = BorderThickness;
        shape.StrokeWidthUnits = DimensionUnitType.Absolute;
        shape.StrokeColor = ForestGladeStyling.ActiveStyle.Colors.Border;
        return shape;
    }

    private static RectangleRuntime NewShape(string name)
    {
        RectangleRuntime shape = new RectangleRuntime();
        shape.Name = name;
        shape.Width = 0;
        shape.Height = 0;
        shape.WidthUnits = DimensionUnitType.RelativeToParent;
        shape.HeightUnits = DimensionUnitType.RelativeToParent;
        shape.CornerRadius = CornerRadius;
        return shape;
    }

    private void WireStates()
    {
        States.Selected.Apply = () => ApplyPalette(ForestGladeStyling.ActiveStyle.Colors.ButtonRestFillTop, ForestGladeStyling.ActiveStyle.Colors.Text);
        States.Unselected.Apply = () => ApplyPalette(ForestGladeStyling.ActiveStyle.Colors.CanopyDeep, ForestGladeStyling.ActiveStyle.Colors.Text);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
    }
}
