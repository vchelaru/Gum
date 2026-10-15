using Gum.Content.AnimationChain;
using Gum.Forms.Controls;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Wireframe;

#if RAYLIB
namespace Examples.Shapes;
#elif SKIA
namespace SilkNetGum.Screens;
#else
namespace MonoGameGumInCode.Screens;
#endif

// One file shared by MonoGameGumInCode, Samples/raylib and SilkNetGumSample (linked into the
// latter two), so the same screen can be opened side by side and a backend rendering bug is not
// confused with sample drift. The only per-backend differences are the namespace, the content
// path prefix, and rows for features a backend does not support yet. Colors are set through
// Red/Green/Blue so no per-backend Color type is needed.
internal class SpriteScreen : FrameworkElement
{
#if RAYLIB
    // raylib loads loose files from the output folder.
    private const string ContentPrefix = "resources\\";
#else
    private const string ContentPrefix = "";
#endif

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
        // Wraps into columns once the stack runs out of height, so the gallery never scrolls
        // off the bottom. Labels are 20% wide (five columns).
        container.WrapsChildren = true;
        this.AddChild(container);

        // Default sprite at native size — PercentageOfSourceFile + Width/Height = 100
        // means "use the texture's own pixel dimensions".
        AddLabel(container, "Default sprite at native size (BearTexture.png):");
        var native = new SpriteRuntime();
        native.SourceFileName = ContentPrefix + "BearTexture.png";
        native.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        native.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        native.Width = 100;
        native.Height = 100;
        container.Children.Add(native);

        // Same sprite at explicit absolute sizes — scaling behavior.
        AddLabel(container, "Explicit absolute sizes (32, 64, 128):");
        var sizesRow = AddRow(container);
        foreach (var size in new[] { 32, 64, 128 })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = ContentPrefix + "BearTexture.png";
            s.Width = size;
            s.Height = size;
            sizesRow.Children.Add(s);
        }

        // TextureAddress.Custom — carve a sub-rectangle out of FrameSheet.png.
        AddLabel(container, "TextureAddress.Custom (carving from FrameSheet.png):");
        var custom = new SpriteRuntime();
        custom.SourceFileName = ContentPrefix + "FrameSheet.png";
        custom.TextureAddress = Gum.Managers.TextureAddress.Custom;
        custom.TextureLeft = 438;
        custom.TextureTop = 231;
        custom.TextureWidth = 42;
        custom.TextureHeight = 42;
        custom.Width = 84;
        custom.Height = 84;
        container.Children.Add(custom);

        // Wrap / tiling (issue #3456) — TextureWidth/Height set to 2x the source file's pixel
        // dimensions (BearTexture.png is 39x40) so the source rectangle extends past the texture
        // bounds. With Wrap=false the area beyond the bounds clamps/stretches; with Wrap=true it
        // repeats the bear 2x2 across the sprite (raylib clamps via software edge-stretching, #3459).
        AddLabel(container, "Wrap (false = clamp/stretch, true = tile, BearTexture.png 2x2):");
        var wrapRow = AddRow(container);
        foreach (var wrap in new[] { false, true })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = ContentPrefix + "BearTexture.png";
            s.TextureAddress = Gum.Managers.TextureAddress.Custom;
            s.TextureLeft = 0;
            s.TextureTop = 0;
            s.TextureWidth = 78;
            s.TextureHeight = 80;
            s.Wrap = wrap;
            s.Width = 78;
            s.Height = 80;
            wrapRow.Children.Add(s);
        }

        // Color tinting.
        AddLabel(container, "Color tinting:");
        var tintRow = AddRow(container);
        foreach (var (red, green, blue) in new[] { (255, 255, 255), (255, 0, 0), (144, 238, 144), (100, 149, 237) })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = ContentPrefix + "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.Red = red;
            s.Green = green;
            s.Blue = blue;
            tintRow.Children.Add(s);
        }

        // SpriteRuntime.ColorOperation (#4792 Gap 1, widened to every backend by #4821) — Modulate
        // (default) multiplies the texture by the red tint, so the bear's detail shows through red;
        // ColorTextureAlpha uses the texture only as an alpha mask and fills with the tint, so the
        // bear reads as a flat red silhouette.
        AddLabel(container, "ColorOperation via SpriteRuntime.ColorOperation property:");
        var colorOpPropertyRow = AddRow(container);
        foreach (var colorOperation in new[]
        {
            RenderingLibrary.Graphics.ColorOperation.Modulate,
            RenderingLibrary.Graphics.ColorOperation.ColorTextureAlpha,
        })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = ContentPrefix + "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.Red = 255;
            s.Green = 0;
            s.Blue = 0;
            s.ColorOperation = colorOperation;
            colorOpPropertyRow.Children.Add(s);
        }

        // ColorOperation.Add via property (#4892) — same white-silhouette effect as the
        // animation-frame row below, but ColorOperation.Add is set directly on
        // SpriteRuntime.ColorOperation with no animation chain involved.
        AddLabel(container, "ColorOperation.Add via property (normal bear, white-silhouette Add bear):");
        var addPropertyRow = AddRow(container);
        foreach (var useAdd in new[] { false, true })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = ContentPrefix + "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            if (useAdd)
            {
                s.Red = 255;
                s.Green = 255;
                s.Blue = 255;
                s.ColorOperation = RenderingLibrary.Graphics.ColorOperation.Add;
            }
            addPropertyRow.Children.Add(s);
        }

        // Add color operation (#4792 Gap 2 on MonoGame, widened to every backend by #4821) — an
        // authored AnimationFrameColorOperation.Add frame with Red/Green/Blue=255 renders as a flat
        // white silhouette (tex.rgb + white saturates to white wherever the texture has any alpha):
        // the MonsterProjectWeb "un-revealed monster" use case from #4792. Left sprite plays the
        // chain normally (no color op authored, so it looks like the plain bear); right sprite's
        // chain frame carries the Add tint.
        AddLabel(container, "Add color operation (normal bear, white-silhouette Add bear):");
        var addRow = AddRow(container);
        var bearTextureSource = new SpriteRuntime();
        bearTextureSource.SourceFileName = ContentPrefix + "BearTexture.png";
        var bearTexture = bearTextureSource.Texture;
        foreach (var useAddTint in new[] { false, true })
        {
            var s = new SpriteRuntime();
            s.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            s.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            s.Width = 100;
            s.Height = 100;

            var chain = new AnimationChain { Name = "AddDemo" };
            var frame = new AnimationFrame { FrameLength = 1.0f, Texture = bearTexture };
            if (useAddTint)
            {
                frame.ColorOperation = AnimationFrameColorOperation.Add;
                frame.Red = 255;
                frame.Green = 255;
                frame.Blue = 255;
            }
            chain.Add(frame);

            var chainList = new AnimationChainList();
            chainList.Add(chain);
            s.AnimationChains = chainList;
            s.CurrentChainName = "AddDemo";

            addRow.Children.Add(s);
        }

        // Per-frame RelativeX glide (#6034) — a looping two-frame chain slides the bear right and back.
        // A backend that ignores animation frame offsets plays it in place.
        AddLabel(container, "Animation frame offset (bear glides right and back):");
        var offsetLane = new ContainerRuntime();
        offsetLane.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        offsetLane.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        offsetLane.Width = 200;
        offsetLane.Height = 64;
        container.Children.Add(offsetLane);

        var offsetSprite = new SpriteRuntime();
        offsetSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        offsetSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        offsetSprite.Width = 64;
        offsetSprite.Height = 64;

        var offsetChain = new AnimationChain { Name = "OffsetDemo" };
        offsetChain.Add(new AnimationFrame
        {
            FrameLength = 1.0f,
            Texture = bearTexture,
            RightCoordinate = 1,
            BottomCoordinate = 1,
            RelativeX = 0,
            OffsetInterpolation = "Linear",
        });
        offsetChain.Add(new AnimationFrame
        {
            FrameLength = 1.0f,
            Texture = bearTexture,
            RightCoordinate = 1,
            BottomCoordinate = 1,
            RelativeX = 136,
            OffsetInterpolation = "Linear",
        });
        var offsetChainList = new AnimationChainList();
        offsetChainList.Add(offsetChain);
        offsetSprite.AnimationChains = offsetChainList;
        offsetSprite.CurrentChainName = "OffsetDemo";
        offsetSprite.Animate = true;
        offsetLane.Children.Add(offsetSprite);

#if !RAYLIB && !SKIA
        // RenderColorOperation authored on an animation frame (#4822) — same Modulate/
        // ColorTextureAlpha technique as the property-driven row above, but driven by the
        // AnimationFrame.RenderColorOperation field instead of setting SpriteRuntime.ColorOperation
        // directly. Proves the .achx/.achj-authoring path, not just the runtime property. MonoGame
        // only: unlike AnimationFrameColorOperation.Add (widened to every backend by #4821),
        // RenderColorOperation is only applied by Sprite.ApplyAnimationFrame on MonoGame/KNI/FNA -
        // raylib and Skia don't read it from a frame yet, so this row stays #if'd out.
        AddLabel(container, "RenderColorOperation via animation frame (Modulate, ColorTextureAlpha):");
        var renderColorOpFrameRow = AddRow(container);
        foreach (var renderColorOperation in new[]
        {
            RenderingLibrary.Graphics.ColorOperation.Modulate,
            RenderingLibrary.Graphics.ColorOperation.ColorTextureAlpha,
        })
        {
            var s = new SpriteRuntime();
            s.Width = 64;
            s.Height = 64;
            s.Red = 255;
            s.Green = 0;
            s.Blue = 0;

            var chain = new AnimationChain { Name = "RenderColorOperationDemo" };
            chain.Add(new AnimationFrame
            {
                FrameLength = 1.0f,
                Texture = bearTexture,
                RenderColorOperation = renderColorOperation,
            });

            var chainList = new AnimationChainList();
            chainList.Add(chain);
            s.AnimationChains = chainList;
            s.CurrentChainName = "RenderColorOperationDemo";

            renderColorOpFrameRow.Children.Add(s);
        }
#endif

        // Alpha — same sprite at 64 / 128 / 192 / 255.
        AddLabel(container, "Alpha (64, 128, 192, 255):");
        var alphaRow = AddRow(container);
        foreach (var alpha in new[] { 64, 128, 192, 255 })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = ContentPrefix + "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.Alpha = alpha;
            alphaRow.Children.Add(s);
        }

        // FlipHorizontal / FlipVertical.
        AddLabel(container, "Flipping (none, horizontal, vertical, both):");
        var flipRow = AddRow(container);
        foreach (var (h, v) in new[] { (false, false), (true, false), (false, true), (true, true) })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = ContentPrefix + "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.FlipHorizontal = h;
            s.FlipVertical = v;
            flipRow.Children.Add(s);
        }

        // Flipping an atlas cell (issue #4854) — a source rect that doesn't start at the texture
        // origin, so a flip that shifts the sample window shows the neighbouring cell instead.
        AddLabel(container, "Flipping an atlas cell (none, horizontal, vertical, both):");
        var flipCellRow = AddRow(container);
        foreach (var (h, v) in new[] { (false, false), (true, false), (false, true), (true, true) })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = ContentPrefix + "FrameSheet.png";
            s.TextureAddress = Gum.Managers.TextureAddress.Custom;
            s.TextureLeft = 438;
            s.TextureTop = 231;
            s.TextureWidth = 42;
            s.TextureHeight = 42;
            s.Width = 64;
            s.Height = 64;
            s.FlipHorizontal = h;
            s.FlipVertical = v;
            flipCellRow.Children.Add(s);
        }

        // Rotation — center-pivot the sprites (YOrigin=Center, Y=row_height/2)
        AddLabel(container, "Rotation (0, 25, 90, 180 degrees):");
        var rotRow = AddRow(container);
        foreach (var angle in new[] { 0f, 25f, 90f, 180f })
        {
            var s = new SpriteRuntime();
            s.SourceFileName = ContentPrefix + "BearTexture.png";
            s.Width = 40;
            s.Height = 40;
            s.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            s.Y = 14;
            s.Rotation = angle;
            rotRow.Children.Add(s);
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
            s.SourceFileName = ContentPrefix + "BearTexture.png";
            s.Width = 64;
            s.Height = 64;
            s.Blend = blend;
            blendRow.Children.Add(s);
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
            redBackground.Red = 255;
            redBackground.Green = 0;
            redBackground.Blue = 0;
            cell.Children.Add(redBackground);

            var alphaMasker = new SpriteRuntime();
            alphaMasker.SourceFileName = ContentPrefix + "BearTexture.png";
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
            cell.Children.Add(alphaMasker);

            alphaBlendRow.Children.Add(cell);
        }

        // AnimationChain-driven sprite — same .achx pipeline NineSliceScreen uses.
        // 200% of source so the frame swaps are easy to see. AnimatedFrame1.achx also
        // authors per-frame Alpha (255 then 60), so the sprite should flash dim every cycle.
        AddLabel(container, "AnimationChain-driven sprite, with per-frame Alpha (AnimatedFrame1.achx):");
        var animated = new SpriteRuntime();
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
        AnimationChainListSave save = AnimationChainListSave.FromFile(ContentPrefix + "AnimatedFrame1.achx");
        return save.ToAnimationChainList();
    }

    private static void AddLabel(ContainerRuntime container, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        label.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        label.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        label.Width = 20;
        label.Height = 0;
        container.Children.Add(label);
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
        container.Children.Add(row);
        return row;
    }
}
