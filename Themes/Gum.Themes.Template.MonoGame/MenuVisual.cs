using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseMenuVisual = Gum.Forms.DefaultVisuals.V3.MenuVisual;

namespace Gum.Themes.Template;

/// <summary>
/// Template-styled Menu visual. Surface1 fill matching the rest of the Template
/// chrome, plus a 1 px Border-colored hairline at the bottom to visually separate
/// the top menu bar from the page body below. V3.Menu has no state callbacks
/// (the MenuCategory state set is empty), so this is static chrome.
/// </summary>
public class MenuVisual : BaseMenuVisual
{
    private const float SeparatorHeight = 1f;

    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _bottomSeparator;

    public MenuVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // V3 order is [Background, InnerPanelInstance]. Replace Background with
        // the Template chrome, and reattach InnerPanelInstance last so menu
        // items render on top of the fill.
        Background.Parent = null;
        InnerPanelInstance.Parent = null;

        _fill = TemplateShapes.Fill(TemplateStyling.ActiveStyle.Colors.Surface1, cornerRadius: 0f, "MenuFill");
        AddChild(_fill);

        // Bottom hairline is an edge-anchored strip, not a full-parent shape, so
        // it's built inline rather than via TemplateShapes.
        _bottomSeparator = CreateBottomSeparator();
        AddChild(_bottomSeparator);

        AddChild(InnerPanelInstance);
    }

    private static RectangleRuntime CreateBottomSeparator()
    {
        RectangleRuntime separator = new RectangleRuntime();
        separator.Name = "MenuBottomSeparator";
        separator.X = 0;
        separator.Y = 0;
        separator.XUnits = GeneralUnitType.PixelsFromMiddle;
        separator.YUnits = GeneralUnitType.PixelsFromLarge;
        separator.XOrigin = HorizontalAlignment.Center;
        separator.YOrigin = VerticalAlignment.Bottom;
        separator.Width = 0;
        separator.Height = SeparatorHeight;
        separator.WidthUnits = DimensionUnitType.RelativeToParent;
        separator.HeightUnits = DimensionUnitType.Absolute;
        separator.IsFilled = true;
        separator.FillColor = TemplateStyling.ActiveStyle.Colors.Border;
        separator.StrokeWidth = 0;
        return separator;
    }
}
