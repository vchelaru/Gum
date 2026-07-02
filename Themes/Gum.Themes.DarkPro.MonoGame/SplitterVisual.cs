using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseSplitterVisual = Gum.Forms.DefaultVisuals.V3.SplitterVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled Splitter visual. A thin Border-colored fill — same hairline
/// look as the rest of Dark Pro's container borders, so a splitter between two
/// surfaces reads as a continuation of the chrome rather than a new control.
/// <para>
/// V3.SplitterVisual has no state category and no hover/press feedback;
/// matching that here keeps the visual minimal. If we later want drag
/// feedback (VS Code-style brighten-on-hover), we'd have to wire mouse events
/// on the InteractiveGue directly since there's no built-in state plumbing.
/// </para>
/// </summary>
public class SplitterVisual : BaseSplitterVisual
{
    private readonly RectangleRuntime _fill;

    public SplitterVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "DarkProSplitterFill";
        fill.X = 0;
        fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.IsFilled = true;
        fill.FillColor = DarkProStyling.ActiveStyle.Colors.Border;
        fill.StrokeWidth = 0;
        return fill;
    }
}
