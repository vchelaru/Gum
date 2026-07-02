using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseSplitterVisual = Gum.Forms.DefaultVisuals.V3.SplitterVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled Splitter visual. AccentLight fill — matches
/// <c>.bb-split-div</c> (a soft pink divider between two panes). V3.Splitter has
/// no state category, so this is static chrome.
/// </summary>
public class SplitterVisual : BaseSplitterVisual
{
    private readonly RectangleRuntime _fill;

    public SplitterVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;

        _fill = BubblegumShapes.Fill(
            color: BubblegumStyling.ActiveStyle.Colors.AccentLight,
            name: "BubblegumSplitterFill");
        AddChild(_fill);
    }
}
