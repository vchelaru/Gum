using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;

namespace Examples.Shapes;

// Raylib mirror of Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/NineSliceScreen.cs (issue #3105).
// Section order and parameter sweeps match the MG version so visual regressions in one backend are
// easy to spot against the other when both samples are run side-by-side. The IsTilingMiddleSections /
// BorderScale rows are the reason this screen exists: those properties are newly supported on the
// raylib NineSlice renderable (previously raylib only stretched the middle bands).
internal class NineSliceScreen : FrameworkElement
{
    public NineSliceScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var page = NewSection(ChildrenLayout.TopToBottomStack, spacing: 4);
        page.X = 4;
        page.Y = 4;
        this.AddChild(page);

        // Default full-texture nine-slice at three sizes so corner/edge/center stretching is visible.
        AddSectionLabel(page, "Default nine-slice (Frame.png) at multiple sizes:");
        var sizesRow = NewSection(ChildrenLayout.LeftToRightStack, spacing: 6);
        page.AddChild(sizesRow);
        foreach (var size in new[] { 32, 64, 96 })
        {
            var ns = new NineSliceRuntime();
            ns.SourceFileName = "resources\\Frame.png";
            ns.Width = size;
            ns.Height = size;
            sizesRow.AddChild(ns);
        }

        // Custom texture address — carve a frame out of FrameSheet.png.
        AddSectionLabel(page, "TextureAddress.Custom (carving from FrameSheet.png):");
        var custom = new NineSliceRuntime();
        custom.SourceFileName = "resources\\FrameSheet.png";
        custom.TextureAddress = TextureAddress.Custom;
        custom.TextureLeft = 438;
        custom.TextureTop = 231;
        custom.TextureWidth = 42;
        custom.TextureHeight = 42;
        custom.Width = 160;
        custom.Height = 64;
        page.AddChild(custom);

        // Color tinting.
        AddSectionLabel(page, "Color tinting:");
        var tintRow = NewSection(ChildrenLayout.LeftToRightStack, spacing: 6);
        page.AddChild(tintRow);
        foreach (var tint in new[] { Color.White, Color.Red, Color.Green, Color.SkyBlue })
        {
            var ns = new NineSliceRuntime();
            ns.SourceFileName = "resources\\SquareFrame.png";
            ns.Width = 56;
            ns.Height = 56;
            ns.Color = tint;
            tintRow.AddChild(ns);
        }

        // IsTilingMiddleSections: stretched (default) vs tiled. This is the headline feature
        // for raylib — the right cell must repeat the middle band instead of stretching it.
        AddSectionLabel(page, "IsTilingMiddleSections (left: stretched, right: tiled):");
        var tilingRow = NewSection(ChildrenLayout.LeftToRightStack, spacing: 6);
        page.AddChild(tilingRow);
        var stretched = new NineSliceRuntime();
        stretched.SourceFileName = "resources\\TilingFrame.png";
        stretched.Width = 220;
        stretched.Height = 56;
        tilingRow.AddChild(stretched);
        var tiled = new NineSliceRuntime();
        tiled.SourceFileName = "resources\\TilingFrame.png";
        tiled.Width = 220;
        tiled.Height = 56;
        tiled.IsTilingMiddleSections = true;
        tilingRow.AddChild(tiled);

        // BorderScale combined with rotation: same source rotated 25 degrees with BorderScale 1
        // (left) and BorderScale 8 (right) so border growth is obvious.
        AddSectionLabel(page, "Rotated (25 deg) with BorderScale 1 and 8:");
        var borderRotRow = NewSection(ChildrenLayout.LeftToRightStack, spacing: 60);
        borderRotRow.Height = 180;
        borderRotRow.HeightUnits = DimensionUnitType.Absolute;
        page.AddChild(borderRotRow);

        var rotScale1 = new NineSliceRuntime();
        rotScale1.SourceFileName = "resources\\Frame.png";
        rotScale1.Width = 120;
        rotScale1.Height = 80;
        rotScale1.BorderScale = 1f;
        rotScale1.Rotation = 25f;
        rotScale1.Y = 50;
        borderRotRow.AddChild(rotScale1);

        var rotScale8 = new NineSliceRuntime();
        rotScale8.SourceFileName = "resources\\Frame.png";
        rotScale8.Width = 120;
        rotScale8.Height = 80;
        rotScale8.BorderScale = 8f;
        rotScale8.Rotation = 25f;
        rotScale8.Y = 50f;
        borderRotRow.AddChild(rotScale8);

        // AnimationChain-driven nine-slice — same .achx pipeline the MG sample and raylib
        // SpriteScreen use. #2911 gave the raylib NineSlice renderable its own AnimationLogic,
        // so the texture swaps across all nine slices every frame change.
        AddSectionLabel(page, "AnimationChain-driven nine-slice (AnimatedFrame1.achx):");
        var animated = new NineSliceRuntime();
        animated.Width = 160;
        animated.Height = 64;
        animated.AnimationChains = LoadAnimatedFrameChain();
        animated.CurrentChainName = "Animation1";
        animated.Animate = true;
        page.AddChild(animated);
    }

    private static AnimationChainList LoadAnimatedFrameChain()
    {
        AnimationChainListSave save = AnimationChainListSave.FromFile("resources\\AnimatedFrame1.achx");
        return save.ToAnimationChainList();
    }

    private static ContainerRuntime NewSection(ChildrenLayout layout, int spacing)
    {
        var section = new ContainerRuntime();
        section.Width = 0;
        section.Height = 0;
        section.WidthUnits = DimensionUnitType.RelativeToChildren;
        section.HeightUnits = DimensionUnitType.RelativeToChildren;
        section.ChildrenLayout = layout;
        section.StackSpacing = spacing;
        return section;
    }

    private static void AddSectionLabel(ContainerRuntime parent, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        // Near-white so the labels read against the dark-blue gallery background, matching
        // the other raylib screens' section headers.
        label.Red = 220;
        label.Green = 220;
        label.Blue = 220;
        label.WidthUnits = DimensionUnitType.RelativeToChildren;
        label.HeightUnits = DimensionUnitType.RelativeToChildren;
        label.Width = 0;
        label.Height = 0;
        parent.AddChild(label);
    }
}
