using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SilkNetGum.Screens;

// Mirror of Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/SpriteScreen.cs and
// Samples/raylib/Screens/SpriteScreen.cs. Section order and parameter sweeps match the
// other two so visual regressions in one backend are easy to spot against the others
// when all three are opened side by side.
//
// Skia-specific: the Blend rows from the MG screen are absent here because
// SpriteRuntime.Blend is #if XNALIKE only (see #2907). Otherwise the surface mirrors
// the raylib version.
internal class SpriteScreen : GraphicalUiElement
{
    public SpriteScreen() : base(new InvisibleRenderable())
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

        // Default sprite at native size — PercentageOfSourceFile + Width/Height = 100
        // means "use the texture's own pixel dimensions".
        AddLabel(container, "Default sprite at native size (BearTexture.png):");
        SpriteRuntime native = new SpriteRuntime();
        native.SourceFileName = "BearTexture.png";
        native.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        native.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        native.Width = 100;
        native.Height = 100;
        container.Children.Add(native);

        // Explicit absolute sizes.
        AddLabel(container, "Explicit absolute sizes (32, 64, 128):");
        ContainerRuntime sizesRow = AddRow(container);
        foreach (int size in new[] { 32, 64, 128 })
        {
            SpriteRuntime s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.Width = size;
            s.Height = size;
            sizesRow.Children.Add(s);
        }

        // TextureAddress.Custom — carve a sub-rectangle out of FrameSheet.png.
        AddLabel(container, "TextureAddress.Custom (carving from FrameSheet.png):");
        SpriteRuntime custom = new SpriteRuntime();
        custom.SourceFileName = "FrameSheet.png";
        custom.TextureAddress = TextureAddress.Custom;
        custom.TextureLeft = 438;
        custom.TextureTop = 231;
        custom.TextureWidth = 42;
        custom.TextureHeight = 42;
        custom.Width = 84;
        custom.Height = 84;
        container.Children.Add(custom);

        // Color tinting.
        AddLabel(container, "Color tinting:");
        ContainerRuntime tintRow = AddRow(container);
        foreach (SKColor tint in new[] { SKColors.White, SKColors.Red, SKColors.LightGreen, SKColors.CornflowerBlue })
        {
            SpriteRuntime s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.Color = tint;
            tintRow.Children.Add(s);
        }

        // Alpha — same sprite at 64 / 128 / 192 / 255.
        AddLabel(container, "Alpha (64, 128, 192, 255):");
        ContainerRuntime alphaRow = AddRow(container);
        foreach (int alpha in new[] { 64, 128, 192, 255 })
        {
            SpriteRuntime s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.Alpha = alpha;
            alphaRow.Children.Add(s);
        }

        // FlipHorizontal / FlipVertical.
        AddLabel(container, "Flipping (none, horizontal, vertical, both):");
        ContainerRuntime flipRow = AddRow(container);
        foreach ((bool h, bool v) in new[] { (false, false), (true, false), (false, true), (true, true) })
        {
            SpriteRuntime s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.FlipHorizontal = h;
            s.FlipVertical = v;
            flipRow.Children.Add(s);
        }

        // Rotation — center-pivot so rotated sprites stay anchored.
        AddLabel(container, "Rotation (0, 25, 90, 180 degrees):");
        ContainerRuntime rotRow = AddRow(container);
        foreach (float angle in new[] { 0f, 25f, 90f, 180f })
        {
            SpriteRuntime s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.Width = 40;
            s.Height = 40;
            s.YOrigin = VerticalAlignment.Center;
            s.Y = 14;
            s.Rotation = angle;
            rotRow.Children.Add(s);
        }

        // AnimationChain-driven sprite — same .achx pipeline the MG/raylib samples use.
        // .achx + tile_0064/0065.png live in Content/GumProject/ and are picked up by
        // the existing Content/GumProject/**/*.* copy-to-output rule in SilkNetGum.csproj.
        // Per-frame texture loads route through the AnimateSelf path that the
        // IAnimatable duplicate-fix in SkiaGum.csproj unblocked.
        AddLabel(container, "AnimationChain-driven sprite (AnimatedFrame1.achx):");
        SpriteRuntime animated = new SpriteRuntime();
        // 200% of source so the frame swaps are easy to see.
        animated.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        animated.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        animated.Width = 200;
        animated.Height = 200;
        animated.AnimationChains = LoadAnimatedFrameChain();
        animated.CurrentChainName = "Animation1";
        animated.Animate = true;
        container.Children.Add(animated);
    }

    private static AnimationChainList LoadAnimatedFrameChain()
    {
        AnimationChainListSave save = AnimationChainListSave.FromFile("AnimatedFrame1.achx");
        return save.ToAnimationChainList();
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
