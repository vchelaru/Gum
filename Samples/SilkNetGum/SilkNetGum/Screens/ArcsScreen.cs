using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SilkNetGum.Screens;

// Issue #2851 — Skia mirror of MonoGameGumShapesGallery/Screens/ArcsScreen.cs. Skia has
// always faded the shadow with the body (the canonical behavior), so this screen exists for
// visual parity with the post-#2851 MonoGame side — same cells, same parameters, so a
// regression on either backend is easy to spot.
internal class ArcsScreen : GraphicalUiElement
{
    public ArcsScreen() : base(new InvisibleRenderable())
    {
        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        root.StackSpacing = 14;
        root.X = 10;
        root.Y = 10;
        this.Children.Add(root);

        root.Children.Add(BuildSection("Sweep angles (90, 180, 270, 360)", BuildSweepRow()));
        root.Children.Add(BuildSection("Dropshadow (off / soft / hard / colored / faded body)", BuildDropshadowRow()));
    }

    static ContainerRuntime BuildSection(string label, GraphicalUiElement body)
    {
        ContainerRuntime section = new();
        section.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        section.StackSpacing = 4;
        section.WidthUnits = DimensionUnitType.RelativeToChildren;
        section.HeightUnits = DimensionUnitType.RelativeToChildren;
        section.Width = 0;
        section.Height = 0;

        TextRuntime header = new();
        header.Text = label;
        header.Red = 220;
        header.Green = 220;
        header.Blue = 220;
        section.Children.Add(header);
        section.Children.Add(body);
        return section;
    }

    static ContainerRuntime BuildHorizontalRow()
    {
        ContainerRuntime row = new();
        row.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 16;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        return row;
    }

    static ContainerRuntime BuildSweepRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float sweep in new[] { 90f, 180f, 270f, 360f })
        {
            ArcRuntime arc = new();
            arc.Width = 60;
            arc.Height = 60;
            arc.SweepAngle = sweep;
            arc.Thickness = 8;
            arc.Color = SKColors.Goldenrod;
            row.Children.Add(arc);
        }
        return row;
    }

    static ContainerRuntime BuildDropshadowRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        ArcRuntime baseline = new();
        baseline.Width = 60; baseline.Height = 60;
        baseline.SweepAngle = 270;
        baseline.Thickness = 10;
        baseline.Color = SKColors.Goldenrod;
        row.Children.Add(baseline);

        ArcRuntime soft = new();
        soft.Width = 60; soft.Height = 60;
        soft.SweepAngle = 270;
        soft.Thickness = 10;
        soft.Color = SKColors.Goldenrod;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 4;
        soft.DropshadowOffsetY = 4;
        soft.DropshadowBlurX = 4;
        soft.DropshadowBlurY = 4;
        row.Children.Add(soft);

        ArcRuntime hard = new();
        hard.Width = 60; hard.Height = 60;
        hard.SweepAngle = 270;
        hard.Thickness = 10;
        hard.Color = SKColors.Goldenrod;
        hard.HasDropshadow = true;
        hard.DropshadowRed = 0; hard.DropshadowGreen = 0; hard.DropshadowBlue = 0; hard.DropshadowAlpha = 160;
        hard.DropshadowOffsetX = 6;
        hard.DropshadowOffsetY = 6;
        hard.DropshadowBlurX = 0;
        hard.DropshadowBlurY = 0;
        row.Children.Add(hard);

        ArcRuntime colored = new();
        colored.Width = 60; colored.Height = 60;
        colored.SweepAngle = 270;
        colored.Thickness = 10;
        colored.Color = SKColors.Goldenrod;
        colored.HasDropshadow = true;
        colored.DropshadowRed = 220; colored.DropshadowGreen = 40; colored.DropshadowBlue = 160; colored.DropshadowAlpha = 220;
        colored.DropshadowOffsetX = 6;
        colored.DropshadowOffsetY = 6;
        colored.DropshadowBlurX = 6;
        colored.DropshadowBlurY = 6;
        row.Children.Add(colored);

        ArcRuntime fadedBody = new();
        fadedBody.Width = 60; fadedBody.Height = 60;
        fadedBody.SweepAngle = 270;
        fadedBody.Thickness = 10;
        fadedBody.Color = new SKColor(218, 165, 32, 80);
        fadedBody.HasDropshadow = true;
        fadedBody.DropshadowOffsetX = 4;
        fadedBody.DropshadowOffsetY = 4;
        fadedBody.DropshadowBlurX = 4;
        fadedBody.DropshadowBlurY = 4;
        row.Children.Add(fadedBody);

        return row;
    }
}
