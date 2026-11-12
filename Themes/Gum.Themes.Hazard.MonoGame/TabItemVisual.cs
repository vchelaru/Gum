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

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard styled TabItem visual: a square-cornered rectangle with a 2px Border outline that is Surface2 when unselected and Accent (with Ink text) when selected.
/// </summary>
public class TabItemVisual : BaseTabItemVisual
{
    internal const float CornerRadius = 0f;
    internal const float BorderThickness = 2f;

    private readonly RectangleRuntime _fill;

    public TabItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Replace the base NineSlice with the theme's fill and border; the text is reparented last so it draws on top.
        Background.Parent = null;
        TextInstance.Parent = null;

        _fill = CreateFill("HazardTabFill", HazardStyling.ActiveStyle.Colors.Surface2);
        AddChild(_fill);
        AddChild(CreateBorder("HazardTabBorder"));
        AddChild(TextInstance);

        WireStates();
        ApplyPalette(HazardStyling.ActiveStyle.Colors.Surface2, HazardStyling.ActiveStyle.Colors.Text);
    }

    // Shared with TabControlVisual so the strip and the content panel use one set of shapes.
    internal static RectangleRuntime CreateFill(string name, Color color) =>
        HazardShapes.Fill(color, CornerRadius, name);

    internal static RectangleRuntime CreateBorder(string name) =>
        HazardShapes.Border(HazardStyling.ActiveStyle.Colors.Border, CornerRadius, BorderThickness, name);

    private void WireStates()
    {
        States.Selected.Apply = () => ApplyPalette(HazardStyling.ActiveStyle.Colors.Accent, HazardStyling.ActiveStyle.Colors.Ink);
        States.Unselected.Apply = () => ApplyPalette(HazardStyling.ActiveStyle.Colors.Surface2, HazardStyling.ActiveStyle.Colors.Text);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
    }
}
