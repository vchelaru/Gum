using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;

namespace MonoGameGumInCode.Screens;
internal class NineSliceScreen : FrameworkElement
{
    public NineSliceScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var container = new ContainerRuntime();
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.X = 4;
        container.Y = 4;
        container.Width = -8;
        container.Height = -8;
        container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        container.StackSpacing = 4;
        this.AddChild(container);

        // Default full-texture nine-slice at three sizes so corner/edge/center
        // stretching is visible.
        AddLabel(container, "Default nine-slice (Frame.png) at multiple sizes:");
        var sizesRow = AddRow(container);
        foreach (var size in new[] { 32, 64, 96 })
        {
            var ns = new NineSliceRuntime();
            ns.SourceFileName = "Frame.png";
            ns.Width = size;
            ns.Height = size;
            sizesRow.AddChild(ns);
        }

        // Custom texture address (carve a frame out of FrameSheet.png).
        AddLabel(container, "TextureAddress.Custom (carving from FrameSheet.png):");
        var custom = new NineSliceRuntime();
        custom.SourceFileName = "FrameSheet.png";
        custom.TextureAddress = Gum.Managers.TextureAddress.Custom;
        custom.TextureLeft = 438;
        custom.TextureTop = 231;
        custom.TextureWidth = 42;
        custom.TextureHeight = 42;
        custom.Width = 160;
        custom.Height = 64;
        container.AddChild(custom);

        // Color tinting demo.
        AddLabel(container, "Color tinting:");
        var tintRow = AddRow(container);
        foreach (var tint in new[] { Color.White, Color.Red, Color.LightGreen, Color.CornflowerBlue })
        {
            var ns = new NineSliceRuntime();
            ns.SourceFileName = "SquareFrame.png";
            ns.Width = 56;
            ns.Height = 56;
            ns.Color = tint;
            tintRow.AddChild(ns);
        }

        // IsTilingMiddleSections: stretched (default) vs tiled.
        AddLabel(container, "IsTilingMiddleSections (left: stretched, right: tiled):");
        var tilingRow = AddRow(container);
        var stretched = new NineSliceRuntime();
        stretched.SourceFileName = "TilingFrame.png";
        stretched.Width = 220;
        stretched.Height = 56;
        tilingRow.AddChild(stretched);
        var tiled = new NineSliceRuntime();
        tiled.SourceFileName = "TilingFrame.png";
        tiled.Width = 220;
        tiled.Height = 56;
        tiled.IsTilingMiddleSections = true;
        tilingRow.AddChild(tiled);

        // BorderScale combined with rotation: same source rotated 25 degrees with
        // BorderScale 1 (left) and BorderScale 8 (right) so border growth is obvious.
        AddLabel(container, "Rotated (25 deg) with BorderScale 1 and 8:");
        var borderRotRow = AddRow(container);
        borderRotRow.StackSpacing = 60;
        borderRotRow.Height = 180;
        borderRotRow.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        var rotScale1 = new NineSliceRuntime();
        rotScale1.SourceFileName = "Frame.png";
        rotScale1.Width = 120;
        rotScale1.Height = 80;
        rotScale1.BorderScale = 1f;
        rotScale1.Rotation = 25f;
        rotScale1.Y = 50;
        borderRotRow.AddChild(rotScale1);

        var rotScale8 = new NineSliceRuntime();
        rotScale8.SourceFileName = "Frame.png";
        rotScale8.Width = 120;
        rotScale8.Height = 80;
        rotScale8.BorderScale = 8f;
        rotScale8.Rotation = 25f;
        rotScale8.Y = 50f;
        borderRotRow.AddChild(rotScale8);
    }

    private static void AddLabel(ContainerRuntime container, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        label.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        label.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        // Width/Height = 0 + RelativeToChildren → exactly fit children. A non-zero
        // value here would be added on top of the children-extent, producing extra
        // padding the layout almost never wants.
        label.Width = 0;
        label.Height = 0;
        container.AddChild(label);
    }

    private static ContainerRuntime AddRow(ContainerRuntime container)
    {
        var row = new ContainerRuntime();
        row.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 6;
        row.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        // Width/Height = 0 + RelativeToChildren → exactly fit children. A non-zero
        // value here would be added on top of the children-extent, producing extra
        // padding the layout almost never wants.
        row.Width = 0;
        row.Height = 0;
        container.AddChild(row);
        return row;
    }
}
