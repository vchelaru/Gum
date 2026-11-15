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

namespace Gum.Themes.Neon;

/// <summary>
/// Neon styled TabItem visual: a 1px-bordered rounded rectangle (CornerRadius=5) that is Surface2 when unselected and a dim Accent wash with Accent text when selected.
/// </summary>
public class TabItemVisual : BaseTabItemVisual
{
    internal const float CornerRadius = 5f;
    internal const float BorderThickness = 1f;

    private readonly RectangleRuntime _fill;

    public TabItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Replace the base NineSlice with the theme's fill and border; the text is reparented last so it draws on top.
        Background.Parent = null;
        TextInstance.Parent = null;

        _fill = CreateFill("NeonTabFill", NeonStyling.ActiveStyle.Colors.Surface2);
        AddChild(_fill);
        AddChild(CreateBorder("NeonTabBorder"));
        AddChild(TextInstance);

        WireStates();
        ApplyPalette(NeonStyling.ActiveStyle.Colors.Surface2, NeonStyling.ActiveStyle.Colors.Text);
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
        shape.StrokeColor = NeonStyling.ActiveStyle.Colors.Border;
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
        States.Selected.Apply = () => ApplyPalette(NeonStyling.ActiveStyle.Colors.SelectedTab, NeonStyling.ActiveStyle.Colors.Accent);
        States.Unselected.Apply = () => ApplyPalette(NeonStyling.ActiveStyle.Colors.Surface2, NeonStyling.ActiveStyle.Colors.Text);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
    }
}
