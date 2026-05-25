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

// Raylib mirror of Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/SpriteScreen.cs (issue #2912).
// Section order and parameter sweeps match the MG version so visual regressions in one backend are
// easy to spot against the other when both samples are run side-by-side.
//
// What's intentionally NOT mirrored: the Blend and alpha-only render-target Blend rows. The Blend
// property on SpriteRuntime is currently #if XNALIKE only — raylib has no Blend surface. Tracked
// in #2907 (SpriteRuntime: unify remaining 3 #if XNALIKE blocks).
internal class SpriteScreen : FrameworkElement
{
    public SpriteScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var page = NewSection(ChildrenLayout.TopToBottomStack, spacing: 4);
        page.X = 4;
        page.Y = 4;
        this.AddChild(page);

        // Default sprite at native size — PercentageOfSourceFile + Width/Height = 100
        // means "use the texture's own pixel dimensions".
        AddSectionLabel(page, "Default sprite at native size (BearTexture.png):");
        var native = new SpriteRuntime();
        native.SourceFileName = "resources\\BearTexture.png";
        native.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        native.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        native.Width = 100;
        native.Height = 100;
        page.AddChild(native);

        // Explicit absolute sizes.
        AddSectionLabel(page, "Explicit absolute sizes (32, 64, 128):");
        var sizesRow = NewSection(ChildrenLayout.LeftToRightStack, spacing: 6);
        page.AddChild(sizesRow);
        foreach (var size in new[] { 32, 64, 128 })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "resources\\BearTexture.png";
            s.Width = size;
            s.Height = size;
            sizesRow.AddChild(s);
        }

        // TextureAddress.Custom — carve a sub-rectangle out of parrots.png.
        AddSectionLabel(page, "TextureAddress.Custom (carving from parrots.png):");
        var custom = new SpriteRuntime();
        custom.SourceFileName = "resources\\parrots.png";
        custom.TextureAddress = TextureAddress.Custom;
        custom.TextureLeft = 0;
        custom.TextureTop = 0;
        custom.TextureWidth = 200;
        custom.TextureHeight = 200;
        custom.Width = 96;
        custom.Height = 96;
        page.AddChild(custom);

        // Color tinting.
        AddSectionLabel(page, "Color tinting:");
        var tintRow = NewSection(ChildrenLayout.LeftToRightStack, spacing: 6);
        page.AddChild(tintRow);
        foreach (var tint in new[] { Color.White, Color.Red, Color.Lime, Color.SkyBlue })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "resources\\BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.Color = tint;
            tintRow.AddChild(s);
        }

        // Alpha — same sprite at 64 / 128 / 192 / 255.
        AddSectionLabel(page, "Alpha (64, 128, 192, 255):");
        var alphaRow = NewSection(ChildrenLayout.LeftToRightStack, spacing: 6);
        page.AddChild(alphaRow);
        foreach (var alpha in new[] { 64, 128, 192, 255 })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "resources\\BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.Alpha = alpha;
            alphaRow.AddChild(s);
        }

        // FlipHorizontal / FlipVertical.
        AddSectionLabel(page, "Flipping (none, horizontal, vertical, both):");
        var flipRow = NewSection(ChildrenLayout.LeftToRightStack, spacing: 6);
        page.AddChild(flipRow);
        foreach (var (h, v) in new[] { (false, false), (true, false), (false, true), (true, true) })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "resources\\BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.FlipHorizontal = h;
            s.FlipVertical = v;
            flipRow.AddChild(s);
        }

        // Rotation — center-pivot so rotated sprites stay anchored. Matches the MG screen.
        AddSectionLabel(page, "Rotation (0, 25, 90, 180 degrees):");
        var rotRow = NewSection(ChildrenLayout.LeftToRightStack, spacing: 6);
        page.AddChild(rotRow);
        foreach (var angle in new[] { 0f, 25f, 90f, 180f })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "resources\\BearTexture.png";
            s.Width = 40;
            s.Height = 40;
            s.YOrigin = VerticalAlignment.Center;
            s.Y = 14;
            s.Rotation = angle;
            rotRow.AddChild(s);
        }

        // AnimationChain-driven sprite — same .achx pipeline the MG sample uses. .achx +
        // tile_0064/0065.png live in resources/ and are picked up by the resources/**/*.*
        // copy-to-output glob in GumTest.csproj. #2909 wired AnimationLogic into the raylib
        // Sprite renderable so this row exercises the full chain on raylib.
        AddSectionLabel(page, "AnimationChain-driven sprite (AnimatedFrame1.achx):");
        var animated = new SpriteRuntime();
        // 200% of source so the frame swaps are easy to see.
        animated.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        animated.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        animated.Width = 200;
        animated.Height = 200;
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
        // Explicit near-white so the labels read against the dark-blue background.
        // RawVisualsScreen's identical helper leaves color at the TextRuntime default,
        // which renders as dark gray on this background; matching RectanglesScreen's
        // 220/220/220 keeps every gallery section header consistent.
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
