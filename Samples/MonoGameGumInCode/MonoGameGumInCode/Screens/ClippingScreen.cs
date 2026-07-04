using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;

namespace MonoGameGumInCode.Screens;

// Visual regression coverage for clip + Apos.Shapes interaction. Shapes go through
// ShapeBatch (Apos.Shapes), sprites through SpriteBatch — when a ClipsChildren scope
// opens or closes mid-walk, both batches must end before the renderer restarts with
// the new scissor or the first shape inside the clip bleeds past the clip edge.
//
// The canary scenarios encoded here mirror the checklist in
// .claude/skills/gum-monogame-rendering "Mid-Walk Scissor Change Must Flush the Open
// Custom Batch":
//
//   1. ScrollViewer with a tall stack of CircleRuntime children. Scroll past the
//      top edge and verify the first child clips at the viewer's content edge
//      (the historical canary — pre-fix the first item bled past the clip).
//   2. A free-floating CircleRuntime placed BEFORE the ScrollViewer in the tree,
//      so by the time the orderer emits BeginClip a ShapeBatch is already open
//      with no-scissor state. The renderer must flush that batch before pushing
//      the new scissor, or shapes inside the clip queue into the stale batch.
//   3. A RoundedRectangleRuntime as the first child inside the clip (so its
//      BatchKey="Apos.Shapes" matches whatever the prior sibling established —
//      the no-op-orchestrator case from #2 must still produce a fresh
//      StartBatch with the new scissor).
//
// Toggling between this and other gallery pages on the nav strip also exercises
// the cross-cycle state (the renderer's clip scope stack must be empty when we
// leave the screen, otherwise the next layer renders with a stale scissor).
internal class ClippingScreen : FrameworkElement
{
    public ClippingScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        // (2) Shape sibling that renders BEFORE the ScrollViewer so its ShapeBatch is
        // open with no-scissor state when the clip scope opens. Without the
        // BeginClipScope FlushAndReset, the first shape inside the scroll viewer
        // would queue into this stale batch and render outside the clip.
        CircleRuntime outsideClipShape = new();
        outsideClipShape.X = 20;
        outsideClipShape.Y = 20;
        outsideClipShape.Radius = 24;
        outsideClipShape.FillColor = Color.OrangeRed;
        outsideClipShape.IsFilled = true;
        AddChild(outsideClipShape);

        ScrollViewer scrollViewer = new ScrollViewer();
        scrollViewer.X = 100;
        scrollViewer.Y = 20;
        scrollViewer.Width = 320;
        scrollViewer.Height = 360;
        scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        scrollViewer.InnerPanel.StackSpacing = 8;
        AddChild(scrollViewer);

        // (3) First child is a shape — exercises the "BatchKey matches but a clip
        // change just happened" path. Pre-fix this circle would render with the
        // outer (no-scissor) state and bleed above the viewer when scrolled.
        for (int i = 0; i < 24; i++)
        {
            ContainerRuntime row = new();
            row.WidthUnits = DimensionUnitType.RelativeToParent;
            row.Width = 0;
            row.HeightUnits = DimensionUnitType.Absolute;
            row.Height = 40;

            RectangleRuntime bg = new();
            bg.WidthUnits = DimensionUnitType.RelativeToParent;
            bg.Width = -16;
            bg.X = 8;
            bg.HeightUnits = DimensionUnitType.Absolute;
            bg.Height = 36;
            bg.Y = 2;
            bg.CornerRadius = 6;
            bg.FillColor = (i % 2 == 0) ? new Color(60, 60, 90) : new Color(80, 80, 120);
            bg.IsFilled = true;
            row.AddChild(bg);

            CircleRuntime dot = new();
            dot.X = 16;
            dot.Y = 8;
            dot.Radius = 12;
            dot.FillColor = Color.Goldenrod;
            dot.IsFilled = true;
            row.AddChild(dot);

            TextRuntime label = new();
            label.X = 48;
            label.Y = 10;
            label.Text = $"Row {i + 1}";
            label.Red = 230;
            label.Green = 230;
            label.Blue = 230;
            row.AddChild(label);

            scrollViewer.InnerPanel.Children.Add(row);
        }
    }
}
