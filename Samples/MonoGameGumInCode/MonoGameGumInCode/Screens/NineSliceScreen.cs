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

        // The screen has more demo rows than fit on a 720px tall area, so wrap
        // everything in a ScrollViewer. Layout-wise the InnerPanel is what
        // children stack inside.
        var scroll = new ScrollViewer();
        scroll.Visual.Dock(Gum.Wireframe.Dock.Fill);
        this.AddChild(scroll);

        var container = new ContainerRuntime();
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        // Leave room for the vertical scrollbar
        container.Width = -20;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        container.StackSpacing = 6;
        scroll.InnerPanel.Children.Add(container);

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

        // BorderScale: scales the corner/edge regions independently of element size.
        MixedScreen.AddText(container, "BorderScale (0.5, 1, 2):");

        var borderScaleRow = new ContainerRuntime();
        borderScaleRow.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        borderScaleRow.StackSpacing = 6;
        borderScaleRow.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        borderScaleRow.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        container.AddChild(borderScaleRow);

        foreach (var scale in new[] { 0.5f, 1f, 2f })
        {
            var ns = new NineSliceRuntime();
            ns.SourceFileName = "Frame.png";
            ns.Width = 140;
            ns.Height = 80;
            ns.BorderScale = scale;
            borderScaleRow.AddChild(ns);
        }

        // Rotated variants: same combinations as above, each rotated 25 degrees.
        // Rotation does not affect layout-occupied size, so the row needs absolute
        // height/spacing or rotated corners will clip into adjacent rows.
        MixedScreen.AddText(container, "Rotated (25 degrees) — default, custom source, tinted, tiled, BorderScale=2:");

        var rotatedRow = new ContainerRuntime();
        rotatedRow.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        rotatedRow.StackSpacing = 40;
        rotatedRow.Height = 180;
        rotatedRow.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        container.AddChild(rotatedRow);

        const float rotation = 25f;

        var rotatedDefault = new NineSliceRuntime();
        rotatedDefault.SourceFileName = "Frame.png";
        rotatedDefault.Width = 96;
        rotatedDefault.Height = 96;
        rotatedDefault.Rotation = rotation;
        rotatedRow.AddChild(rotatedDefault);

        var rotatedCustom = new NineSliceRuntime();
        rotatedCustom.SourceFileName = "FrameSheet.png";
        rotatedCustom.TextureAddress = Gum.Managers.TextureAddress.Custom;
        rotatedCustom.TextureLeft = 438;
        rotatedCustom.TextureTop = 231;
        rotatedCustom.TextureWidth = 42;
        rotatedCustom.TextureHeight = 42;
        rotatedCustom.Width = 96;
        rotatedCustom.Height = 96;
        rotatedCustom.Rotation = rotation;
        rotatedRow.AddChild(rotatedCustom);

        var rotatedTinted = new NineSliceRuntime();
        rotatedTinted.SourceFileName = "SquareFrame.png";
        rotatedTinted.Width = 96;
        rotatedTinted.Height = 96;
        rotatedTinted.Color = Color.Red;
        rotatedTinted.Rotation = rotation;
        rotatedRow.AddChild(rotatedTinted);

        var rotatedTiled = new NineSliceRuntime();
        rotatedTiled.SourceFileName = "Frame.png";
        rotatedTiled.Width = 140;
        rotatedTiled.Height = 64;
        rotatedTiled.IsTilingMiddleSections = true;
        rotatedTiled.Rotation = rotation;
        rotatedRow.AddChild(rotatedTiled);

        var rotatedBorderScale = new NineSliceRuntime();
        rotatedBorderScale.SourceFileName = "Frame.png";
        rotatedBorderScale.Width = 120;
        rotatedBorderScale.Height = 80;
        rotatedBorderScale.BorderScale = 2f;
        rotatedBorderScale.Rotation = rotation;
        rotatedRow.AddChild(rotatedBorderScale);
    }
}
