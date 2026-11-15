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

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum styled TabItem visual: a 2px-bordered rounded rectangle (CornerRadius=8) that is HoverRow when unselected and the AccentLight selected-row pink with AccentDark text when selected.
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

        _fill = CreateFill("BubblegumTabFill", BubblegumStyling.ActiveStyle.Colors.HoverRow);
        AddChild(_fill);
        AddChild(CreateBorder("BubblegumTabBorder"));
        AddChild(TextInstance);

        WireStates();
        ApplyPalette(BubblegumStyling.ActiveStyle.Colors.HoverRow, BubblegumStyling.ActiveStyle.Colors.Text);
    }

    // Shared with TabControlVisual so the strip and the content panel use one set of shapes.
    internal static RectangleRuntime CreateFill(string name, Color color) =>
        BubblegumShapes.Fill(color, CornerRadius, name);

    internal static RectangleRuntime CreateBorder(string name) =>
        BubblegumShapes.Border(BubblegumStyling.ActiveStyle.Colors.Border, CornerRadius, BorderThickness, name);

    private void WireStates()
    {
        States.Selected.Apply = () => ApplyPalette(BubblegumStyling.ActiveStyle.Colors.SelectedRow, BubblegumStyling.ActiveStyle.Colors.SelectedRowText);
        States.Unselected.Apply = () => ApplyPalette(BubblegumStyling.ActiveStyle.Colors.HoverRow, BubblegumStyling.ActiveStyle.Colors.Text);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
    }
}
