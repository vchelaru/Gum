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

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow styled TabItem visual: a soft rounded rectangle (CornerRadius=8) with a 2px PeachDark outline that is PeachLight when unselected and Blue (with white text) when selected.
/// </summary>
public class TabItemVisual : BaseTabItemVisual
{
    internal const float CornerRadius = 8f;
    internal const float BorderThickness = 2f;

    private readonly RectangleRuntime _fill;

    public TabItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Replace the base NineSlice with the theme's fill and border; the text is reparented last so it draws on top.
        Background.Parent = null;
        TextInstance.Parent = null;

        _fill = CreateFill("MeadowTabFill", MeadowStyling.ActiveStyle.Colors.PeachLight);
        AddChild(_fill);
        AddChild(CreateBorder("MeadowTabBorder"));
        AddChild(TextInstance);

        WireStates();
        ApplyPalette(MeadowStyling.ActiveStyle.Colors.PeachLight, MeadowStyling.ActiveStyle.Colors.TealDark);
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
        shape.StrokeColor = MeadowStyling.ActiveStyle.Colors.PeachDark;
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
        States.Selected.Apply = () => ApplyPalette(MeadowStyling.ActiveStyle.Colors.Blue, MeadowStyling.ActiveStyle.Colors.White);
        States.Unselected.Apply = () => ApplyPalette(MeadowStyling.ActiveStyle.Colors.PeachLight, MeadowStyling.ActiveStyle.Colors.TealDark);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
    }
}
