using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SilkNetGum.Screens;

/// <summary>
/// Mirror of <c>MonoGameGumInCode.Screens.NineSliceScreen</c>, adapted for the
/// SkiaGum Silk.NET host so the two samples can be opened side by side to compare
/// the unified <see cref="NineSliceRuntime"/> rendering between MonoGame and Skia.
/// </summary>
internal class NineSliceScreen : GraphicalUiElement
{
    public NineSliceScreen() : base(new InvisibleRenderable())
    {
        this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Width = 0;
        this.Height = 0;

        ContainerRuntime container = new ContainerRuntime();
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.X = 4;
        container.Y = 4;
        container.Width = -8;
        container.Height = -8;
        container.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        container.StackSpacing = 4;
        this.Children.Add(container);

        AddLabel(container, "Default nine-slice (Frame.png) at multiple sizes:");
        ContainerRuntime sizesRow = AddRow(container);
        foreach (int size in new[] { 32, 64, 96 })
        {
            NineSliceRuntime ns = new NineSliceRuntime();
            ns.SourceFileName = "Frame.png";
            ns.Width = size;
            ns.Height = size;
            sizesRow.Children.Add(ns);
        }

        AddLabel(container, "TextureAddress.Custom (carving from FrameSheet.png):");
        NineSliceRuntime custom = new NineSliceRuntime();
        custom.SourceFileName = "FrameSheet.png";
        custom.TextureAddress = TextureAddress.Custom;
        custom.TextureLeft = 438;
        custom.TextureTop = 231;
        custom.TextureWidth = 42;
        custom.TextureHeight = 42;
        custom.Width = 160;
        custom.Height = 64;
        container.Children.Add(custom);

        AddLabel(container, "Color tinting:");
        ContainerRuntime tintRow = AddRow(container);
        foreach (SKColor tint in new[] { SKColors.White, SKColors.Red, SKColors.LightGreen, SKColors.CornflowerBlue })
        {
            NineSliceRuntime ns = new NineSliceRuntime();
            ns.SourceFileName = "SquareFrame.png";
            ns.Width = 56;
            ns.Height = 56;
            ns.Color = tint;
            tintRow.Children.Add(ns);
        }

        AddLabel(container, "IsTilingMiddleSections (left: stretched, right: tiled):");
        ContainerRuntime tilingRow = AddRow(container);
        NineSliceRuntime stretched = new NineSliceRuntime();
        stretched.SourceFileName = "TilingFrame.png";
        stretched.Width = 220;
        stretched.Height = 56;
        tilingRow.Children.Add(stretched);
        NineSliceRuntime tiled = new NineSliceRuntime();
        tiled.SourceFileName = "TilingFrame.png";
        tiled.Width = 220;
        tiled.Height = 56;
        tiled.IsTilingMiddleSections = true;
        tilingRow.Children.Add(tiled);

        AddLabel(container, "Rotated (25 deg) with BorderScale 1 and 8:");
        ContainerRuntime borderRotRow = AddRow(container);
        borderRotRow.StackSpacing = 60;
        borderRotRow.Height = 180;
        borderRotRow.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        NineSliceRuntime rotScale1 = new NineSliceRuntime();
        rotScale1.SourceFileName = "Frame.png";
        rotScale1.Width = 120;
        rotScale1.Height = 80;
        rotScale1.BorderScale = 1f;
        rotScale1.Rotation = 25f;
        rotScale1.Y = 50;
        borderRotRow.Children.Add(rotScale1);

        NineSliceRuntime rotScale8 = new NineSliceRuntime();
        rotScale8.SourceFileName = "Frame.png";
        rotScale8.Width = 120;
        rotScale8.Height = 80;
        rotScale8.BorderScale = 8f;
        rotScale8.Rotation = 25f;
        rotScale8.Y = 50f;
        borderRotRow.Children.Add(rotScale8);
    }

    private static void AddLabel(ContainerRuntime container, string text)
    {
        TextRuntime label = new TextRuntime();
        label.Text = text;
        label.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        label.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        container.Children.Add(label);
    }

    private static ContainerRuntime AddRow(ContainerRuntime container)
    {
        ContainerRuntime row = new ContainerRuntime();
        row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 6;
        row.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.Height = 0;
        container.Children.Add(row);
        return row;
    }
}
