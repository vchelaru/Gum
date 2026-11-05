using Gum.GueDeriving;
using Gum.Wireframe;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Keeps a label-sized button one line tall when its label is empty. An empty Text measures
/// no lines, so a button sized by its children would collapse to its padding. The probe is a
/// transparent single space in the label's font; it counts as one line, and a wrapped label
/// that needs more lines simply outgrows it. Copy the label's font settings again if they
/// change after construction.
/// </summary>
internal static class ForestGladeLabelLineProbe
{
    internal const string ProbeName = "LineHeightProbe";

    internal static TextRuntime AddTo(GraphicalUiElement host, TextRuntime label)
    {
        TextRuntime probe = new TextRuntime();
        probe.Name = ProbeName;
        probe.Text = " ";
        probe.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        probe.Alpha = 0;
        probe.Width = 0;
        probe.WidthUnits = label.WidthUnits;
        probe.Height = 0;
        probe.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        probe.XUnits = label.XUnits;
        probe.YUnits = label.YUnits;
        probe.XOrigin = label.XOrigin;
        probe.YOrigin = label.YOrigin;
        host.AddChild(probe);
        return probe;
    }
}
