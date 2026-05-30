using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;

namespace GumShapesGallery.Screens;

// Stress screen for the cross-batch-type alternation pattern that the
// BatchKeyGroupedOrderer (issue #2879) exists to fix. Each row contains:
//   - a ColoredRectangleRuntime (renders via SpriteBatch)
//   - a TextRuntime              (renders via SpriteBatch)
//   - a CircleRuntime            (renders via Apos.Shapes)
//
// In DFS order this produces SpriteBatch -> SpriteBatch -> Apos.Shapes per row,
// forcing two batch transitions per row (Apos.Shapes -> SpriteBatch at the start
// of the next row, then SpriteBatch -> Apos.Shapes mid-row). With 100 rows the
// current renderer racks up a couple hundred batch starts; the overlay on Game1
// shows the SpriteBatch.Begin count so before/after numbers can be compared
// once the orderer lands.
//
// No texture-backed Sprite is used because the sample doesn't ship one;
// ColoredRectangleRuntime is the same code path for batching purposes (single-
// pixel-texture SpriteBatch draw).
internal class BatchMixStressScreen : FrameworkElement
{
    private const int RowCount = 100;
    private const float RowHeight = 40;
    private const float RowSpacing = 4;

    public BatchMixStressScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ScrollViewer scrollViewer = new ScrollViewer();
        scrollViewer.Dock(Gum.Wireframe.Dock.Fill);
        scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        scrollViewer.InnerPanel.StackSpacing = RowSpacing;
        AddChild(scrollViewer);

        for (int i = 0; i < RowCount; i++)
        {
            ContainerRuntime row = new ContainerRuntime();
            row.WidthUnits = DimensionUnitType.RelativeToParent;
            row.Width = -16;
            row.HeightUnits = DimensionUnitType.Absolute;
            row.Height = RowHeight;

            ColoredRectangleRuntime frame = new ColoredRectangleRuntime();
            frame.WidthUnits = DimensionUnitType.RelativeToParent;
            frame.Width = 0;
            frame.HeightUnits = DimensionUnitType.RelativeToParent;
            frame.Height = 0;
            frame.Color = (i % 2 == 0) ? new Color(60, 60, 90) : new Color(80, 80, 120);
            row.AddChild(frame);

            TextRuntime label = new TextRuntime();
            label.X = 12;
            label.Y = 10;
            label.Text = $"Item {i + 1}";
            label.Red = 230;
            label.Green = 230;
            label.Blue = 230;
            row.AddChild(label);

            CircleRuntime dot = new CircleRuntime();
            dot.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
            dot.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            dot.X = -12;
            dot.Y = 8;
            dot.Radius = 12;
            dot.FillColor = Color.Goldenrod;
            dot.IsFilled = true;
            row.AddChild(dot);

            scrollViewer.InnerPanel.Children.Add(row);
        }
    }
}
