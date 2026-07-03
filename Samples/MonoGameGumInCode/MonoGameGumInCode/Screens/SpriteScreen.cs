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

        // Wrap / tiling (issue #3456) — TextureWidth/Height set to 2x the source file's pixel
        // dimensions (BearTexture.png is 39x40) so the source rectangle extends past the texture
        // bounds. With Wrap=false the area beyond the bounds clamps/stretches; with Wrap=true it
        // repeats the bear 2x2 across the sprite. Compare against the raylib mirror of this screen,
        // which now matches (issue #3459 — raylib's Wrap=false clamps via software edge-stretching).
        AddLabel(container, "Wrap (false = clamp/stretch, true = tile, BearTexture.png 2x2):");
        var wrapRow = AddRow(container);
        foreach (var wrap in new[] { false, true })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.TextureAddress = Gum.Managers.TextureAddress.Custom;
            s.TextureLeft = 0;
            s.TextureTop = 0;
            s.TextureWidth = 78;
            s.TextureHeight = 80;
            s.Wrap = wrap;
            s.Width = 78;
            s.Height = 80;
            wrapRow.AddChild(s);
        }

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
        AddLabel(container, "Rotation (0, 25, 90, 180 degrees):");
        var rotRow = AddRow(container);
        foreach (var angle in new[] { 0f, 25f, 90f, 180f })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = "BearTexture.png";
            s.Width = 40;
            s.Height = 40;
            s.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            s.Y = 14;
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

        // Alpha-only blends — each cell is a render-target Container holding a
        // red rectangle as the underlying layer, then a bear sprite on top with
        // the alpha-only Blend modifying that rectangle's alpha per pixel.
        // SubtractAlpha punches a hole; ReplaceAlpha overwrites alpha with the
        // bear's alpha; MinAlpha keeps whichever alpha is lower.
        AddLabel(container, "Blend, alpha-only on render-target Container (SubtractAlpha, ReplaceAlpha, MinAlpha):");
        var alphaBlendRow = AddRow(container);
        foreach (var blend in new[]
        {
            Gum.RenderingLibrary.Blend.SubtractAlpha,
            Gum.RenderingLibrary.Blend.ReplaceAlpha,
            Gum.RenderingLibrary.Blend.MinAlpha,
        })
        {
            var cell = new ContainerRuntime();
            cell.IsRenderTarget = true;
            cell.Width = 32;
            cell.Height = 32;

            // TODO: switch to RectangleRuntime + FillColor once Apos.Shapes is
            // linked into this sample (see #2811 / GUM002 migration). Until
            // then, ColoredRectangleRuntime is the only built-in solid-color
            // rect that ships without the shape runtime dependency.
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete
            var redBackground = new ColoredRectangleRuntime();
#pragma warning restore CS0618
            redBackground.Width = 32;
            redBackground.Height = 32;
            redBackground.Color = Color.Red;
            cell.AddChild(redBackground);

            var alphaMasker = new SpriteRuntime();
            alphaMasker.SourceFileName = "BearTexture.png";
            alphaMasker.Width = 64;
            alphaMasker.Height = 64;
            alphaMasker.Blend = blend;
            // MinAlpha cell only: drop the BEAR's own alpha to 128 (background stays fully
            // opaque at 255). This makes min() actually pick a *different* side depending on
            // region rather than always landing on the same one: inside the bear,
            // min(255, 128) = 128 (the bear's reduced alpha wins over the opaque background);
            // outside the bear (fully transparent PNG area), min(255, 0) = 0 (fully punched
            // through); at the bear's anti-aliased edge, the texture's own partial alpha gives
            // a third, in-between level. Three visibly distinct bands from one texture, no
            // gradient asset needed. (Previously this dropped the *background*'s alpha to 128
            // instead, which only ever showed two flat levels — min(128, 255) always resolved
            // to the same fixed 128 everywhere the bear covered.)
            if (blend == Gum.RenderingLibrary.Blend.MinAlpha)
            {
                alphaMasker.Alpha = 128;
            }
            cell.AddChild(alphaMasker);

            alphaBlendRow.AddChild(cell);
        }

        // AnimationChain-driven sprite — same .achx pipeline NineSliceScreen uses.
        // 200% of source so the frame swaps are easy to see.
        AddLabel(container, "AnimationChain-driven sprite (AnimatedFrame1.achx):");
        var animated = new SpriteRuntime();
        animated.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        animated.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        animated.Width = 200;
        animated.Height = 200;
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
