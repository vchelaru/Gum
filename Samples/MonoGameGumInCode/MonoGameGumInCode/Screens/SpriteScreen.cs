using Gum.Content.AnimationChain;
using Gum.Forms.Controls;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;

namespace MonoGameGumInCode.Screens;
internal class SpriteScreen : FrameworkElement
{
    public SpriteScreen() : base(new ContainerRuntime())
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

        // Default sprite at native size — PercentageOfSourceFile + Width/Height = 100
        // means "use the texture's own pixel dimensions".
        AddLabel(container, "Default sprite at native size (BearTexture.png):");
        var native = new SpriteRuntime();
        native.SourceFileName = "BearTexture.png";
        native.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        native.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        native.Width = 100;
        native.Height = 100;
        container.AddChild(native);

        // Same sprite at explicit absolute sizes — scaling behavior.
        AddLabel(container, "Explicit absolute sizes (32, 64, 128):");
        var sizesRow = AddRow(container);
        foreach (var size in new[] { 32, 64, 128 })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.Width = size;
            s.Height = size;
            sizesRow.AddChild(s);
        }

        // TextureAddress.Custom — carve a sub-rectangle out of FrameSheet.png.
        AddLabel(container, "TextureAddress.Custom (carving from FrameSheet.png):");
        var custom = new SpriteRuntime();
        custom.SourceFileName = "FrameSheet.png";
        custom.TextureAddress = Gum.Managers.TextureAddress.Custom;
        custom.TextureLeft = 438;
        custom.TextureTop = 231;
        custom.TextureWidth = 42;
        custom.TextureHeight = 42;
        custom.Width = 84;
        custom.Height = 84;
        container.AddChild(custom);

        // Color tinting.
        AddLabel(container, "Color tinting:");
        var tintRow = AddRow(container);
        foreach (var tint in new[] { Color.White, Color.Red, Color.LightGreen, Color.CornflowerBlue })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.Color = tint;
            tintRow.AddChild(s);
        }

        // Alpha — same sprite at 64 / 128 / 192 / 255.
        AddLabel(container, "Alpha (64, 128, 192, 255):");
        var alphaRow = AddRow(container);
        foreach (var alpha in new[] { 64, 128, 192, 255 })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.Alpha = alpha;
            alphaRow.AddChild(s);
        }

        // FlipHorizontal / FlipVertical.
        AddLabel(container, "Flipping (none, horizontal, vertical, both):");
        var flipRow = AddRow(container);
        foreach (var (h, v) in new[] { (false, false), (true, false), (false, true), (true, true) })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.FlipHorizontal = h;
            s.FlipVertical = v;
            flipRow.AddChild(s);
        }

        // Rotation — center-pivot the sprites (YOrigin=Center, Y=row_height/2)
        // so they rotate around their own middle. With the default top-left
        // pivot, a 25-deg rotation drifts the visible body upward and overlaps
        // the label above. A 48-sprite rotated 25 deg has a ~64 px bounding
        // box (48 * (cos25 + sin25)), so 70 px absolute fits all angles with
        // a few px of breathing room and stays close to the flip row's height.
        AddLabel(container, "Rotation (0, 25, 90, 180 degrees):");
        var rotRow = AddRow(container);
        rotRow.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        rotRow.Height = 70;
        foreach (var angle in new[] { 0f, 25f, 90f, 180f })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.Width = 48;
            s.Height = 48;
            s.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            s.Y = 35;
            s.Rotation = angle;
            rotRow.AddChild(s);
        }

        // Blend modes — Normal/Additive/Replace are the only blends safe in
        // normal rendering. The three alpha-only blends (SubtractAlpha,
        // ReplaceAlpha, MinAlpha) operate on the alpha of the underlying target
        // and either draw as pure black or invisible outside a render target,
        // so they are demoed below in their own row.
        AddLabel(container, "Blend, normal rendering (Normal, Additive, Replace):");
        var blendRow = AddRow(container);
        foreach (var blend in new[]
        {
            Gum.RenderingLibrary.Blend.Normal,
            Gum.RenderingLibrary.Blend.Additive,
            Gum.RenderingLibrary.Blend.Replace,
        })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.Blend = blend;
            blendRow.AddChild(s);
        }

        // Alpha-only blends on a render-target Container. Each cell is a
        // ContainerRuntime with IsRenderTarget=true holding two sprites: the
        // bear renders normally as the underlying layer, then a smaller bear
        // with the alpha-only Blend punches/replaces/mins the alpha of that
        // layer. When the container composites onto the screen, the
        // alpha-modified region is visible as a cutout / mask / replace.
        AddLabel(container, "Blend, alpha-only on render-target Container (SubtractAlpha, ReplaceAlpha, MinAlpha):");
        var renderTargetRow = AddRow(container);
        foreach (var blend in new[]
        {
            Gum.RenderingLibrary.Blend.SubtractAlpha,
            Gum.RenderingLibrary.Blend.ReplaceAlpha,
            Gum.RenderingLibrary.Blend.MinAlpha,
        })
        {
            var renderTarget = new ContainerRuntime();
            renderTarget.IsRenderTarget = true;
            renderTarget.Width = 64;
            renderTarget.Height = 64;

            var underlying = new SpriteRuntime();
            underlying.SourceFileName = "BearTexture.png";
            underlying.Width = 64;
            underlying.Height = 64;
            renderTarget.AddChild(underlying);

            var alphaMasker = new SpriteRuntime();
            alphaMasker.SourceFileName = "BearTexture.png";
            alphaMasker.Width = 32;
            alphaMasker.Height = 32;
            alphaMasker.X = 16;
            alphaMasker.Y = 16;
            alphaMasker.Blend = blend;
            renderTarget.AddChild(alphaMasker);

            renderTargetRow.AddChild(renderTarget);
        }

        // AnimationChain-driven sprite — same .achx pipeline NineSliceScreen uses.
        AddLabel(container, "AnimationChain-driven sprite (AnimatedFrame1.achx):");
        var animated = new SpriteRuntime();
        animated.Width = 64;
        animated.Height = 64;
        animated.AnimationChains = LoadAnimatedFrameChain();
        animated.CurrentChainName = "Animation1";
        animated.Animate = true;
        container.AddChild(animated);
    }

    private static AnimationChainList LoadAnimatedFrameChain()
    {
        AnimationChainListSave save = AnimationChainListSave.FromFile("AnimatedFrame1.achx");
        return save.ToAnimationChainList();
    }

    private static void AddLabel(ContainerRuntime container, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        label.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        label.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
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
        row.Width = 0;
        row.Height = 0;
        container.AddChild(row);
        return row;
    }
}
