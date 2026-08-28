using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;

namespace MonoGameGumInCode.Screens;

// Repro/demo for issue #2697: a StackPanel of many rows, each a NineSlice frame with a Text
// label on top. Frame and Text are both BatchKey="SpriteBatch" (see gum-monogame-rendering skill)
// so this never triggers the Apos.Shapes<->SpriteBatch batcher switch - the point is that
// SpriteBatch's own consecutive-same-texture batching still can't merge across the frame texture
// and the font atlas, so every row costs 2 real GPU draw calls. A ListBox would hide this by
// culling offscreen rows; this screen deliberately renders every row unculled so the draw-call
// count reflects the whole list, matching a game that wants a StackPanel's non-virtualized layout.
//
// Tick(elapsedSeconds) is unused today (no animation) but present for symmetry with TextScreen and
// in case a future variant wants to grow/shrink the row count live.
internal class DrawCallStressScreen : FrameworkElement
{
    private readonly TextRuntime _drawCallLabel;

    public DrawCallStressScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        _drawCallLabel = new TextRuntime();
        _drawCallLabel.X = 4;
        _drawCallLabel.Y = 4;
        _drawCallLabel.Color = Color.White;
        this.AddChild(_drawCallLabel);

        var stack = new ContainerRuntime();
        stack.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        stack.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        stack.X = 4;
        stack.Y = 28;
        stack.Width = -8;
        stack.Height = -32;
        stack.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        stack.ClipsChildren = true;
        this.AddChild(stack);

        const int rowCount = 40;
        for (int i = 0; i < rowCount; i++)
        {
            var row = new NineSliceRuntime();
            row.SourceFileName = "Frame.png";
            row.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            row.Width = 0;
            row.Height = 32;

            var label = new TextRuntime();
            label.Text = $"Row {i}: frame + text, both SpriteBatch-keyed";
            label.X = 8;
            label.Y = 6;
            row.Children.Add(label);

            stack.Children.Add(row);
        }

        UpdateDrawCallLabel();
    }

    public void Tick(double elapsedSeconds)
    {
        UpdateDrawCallLabel();
    }

    private void UpdateDrawCallLabel()
    {
        int drawCalls = SystemManagers.Default.Renderer.RenderStateChangeStatistics.DrawCallCount;
        _drawCallLabel.Text = $"Draw calls last frame: {drawCalls}";
    }
}
