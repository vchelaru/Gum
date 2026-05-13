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
        container.StackSpacing = 6;
        this.AddChild(container);

        // Default full-texture nine-slice at three sizes so corner/edge/center
        // stretching is visible.
        MixedScreen.AddText(container, "Default nine-slice (Frame.png) at multiple sizes:");

        var sizesRow = new ContainerRuntime();
        sizesRow.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        sizesRow.StackSpacing = 6;
        sizesRow.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        sizesRow.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        container.AddChild(sizesRow);

        foreach (var size in new[] { 48, 96, 192 })
        {
            var ns = new NineSliceRuntime();
            ns.SourceFileName = "Frame.png";
            ns.Width = size;
            ns.Height = size;
            sizesRow.AddChild(ns);
        }

        // Custom texture address (carve a frame out of FrameSheet.png).
        MixedScreen.AddText(container, "TextureAddress.Custom (carving from FrameSheet.png):");

        var custom = new NineSliceRuntime();
        custom.SourceFileName = "FrameSheet.png";
        custom.TextureAddress = Gum.Managers.TextureAddress.Custom;
        custom.TextureLeft = 438;
        custom.TextureTop = 231;
        custom.TextureWidth = 42;
        custom.TextureHeight = 42;
        custom.Width = 160;
        custom.Height = 80;
        container.AddChild(custom);

        // Color tinting demo.
        MixedScreen.AddText(container, "Color tinting:");

        var tintRow = new ContainerRuntime();
        tintRow.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        tintRow.StackSpacing = 6;
        tintRow.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        tintRow.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        container.AddChild(tintRow);

        foreach (var tint in new[] { Color.White, Color.Red, Color.LightGreen, Color.CornflowerBlue })
        {
            var ns = new NineSliceRuntime();
            ns.SourceFileName = "SquareFrame.png";
            ns.Width = 80;
            ns.Height = 80;
            ns.Color = tint;
            tintRow.AddChild(ns);
        }

        // IsTilingMiddleSections: stretched (default) vs tiled.
        MixedScreen.AddText(container, "IsTilingMiddleSections (left: stretched, right: tiled):");

        var tilingRow = new ContainerRuntime();
        tilingRow.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        tilingRow.StackSpacing = 6;
        tilingRow.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        tilingRow.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        container.AddChild(tilingRow);

        var stretched = new NineSliceRuntime();
        stretched.SourceFileName = "Frame.png";
        stretched.Width = 220;
        stretched.Height = 64;
        tilingRow.AddChild(stretched);

        var tiled = new NineSliceRuntime();
        tiled.SourceFileName = "Frame.png";
        tiled.Width = 220;
        tiled.Height = 64;
        tiled.IsTilingMiddleSections = true;
        tilingRow.AddChild(tiled);
    }
}
