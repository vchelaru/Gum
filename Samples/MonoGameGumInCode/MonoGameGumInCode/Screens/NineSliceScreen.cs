using Gum.Content.AnimationChain;
using Gum.Forms.Controls;
using Gum.Graphics.Animation;
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
        container.StackSpacing = 4;
        // Wraps into columns once the stack runs out of height, so the gallery never scrolls
        // off the bottom. Labels are 20% wide (five columns).
        container.WrapsChildren = true;
        this.AddChild(container);

        // Default full-texture nine-slice at three sizes so corner/edge/center
        // stretching is visible.
        AddLabel(container, "Default nine-slice (Frame.png) at multiple sizes:");
        var sizesRow = AddRow(container);
        foreach (var size in new[] { 32, 64, 96 })
        {
            var ns = new NineSliceRuntime();
            ns.SourceFileName = "Frame.png";
            ns.Width = size;
            ns.Height = size;
            sizesRow.AddChild(ns);
        }

        // Custom texture address (carve a frame out of FrameSheet.png).
        AddLabel(container, "TextureAddress.Custom (carving from FrameSheet.png):");
        var custom = new NineSliceRuntime();
        custom.SourceFileName = "FrameSheet.png";
        custom.TextureAddress = Gum.Managers.TextureAddress.Custom;
        custom.TextureLeft = 438;
        custom.TextureTop = 231;
        custom.TextureWidth = 42;
        custom.TextureHeight = 42;
        custom.Width = 160;
        custom.Height = 64;
        container.AddChild(custom);

        // Color tinting demo.
        AddLabel(container, "Color tinting:");
        var tintRow = AddRow(container);
        foreach (var tint in new[] { Color.White, Color.Red, Color.LightGreen, Color.CornflowerBlue })
        {
            var ns = new NineSliceRuntime();
            ns.SourceFileName = "SquareFrame.png";
            ns.Width = 56;
            ns.Height = 56;
            ns.Color = tint;
            tintRow.AddChild(ns);
        }

        // ColorOperation.Add via property (#4892) — same white-silhouette effect as the
        // animation-frame row below, but ColorOperation.Add is set directly on
        // NineSliceRuntime.ColorOperation with no animation chain involved. Mirrors the
        // raylib/SilkNetGum NineSliceScreen's identical row.
        AddLabel(container, "ColorOperation.Add via property (normal frame, white-silhouette Add frame):");
        var addPropertyRow = AddRow(container);
        foreach (var useAdd in new[] { false, true })
        {
            var ns = new NineSliceRuntime();
            ns.SourceFileName = "SquareFrame.png";
            ns.Width = 56;
            ns.Height = 56;
            if (useAdd)
            {
                ns.Color = Color.White;
                ns.ColorOperation = RenderingLibrary.Graphics.ColorOperation.Add;
            }
            addPropertyRow.AddChild(ns);
        }

        // Add color operation (#4821 gap 4) — NineSlice never got the Sprite.cs Add-color treatment
        // (#4792 Gap 2) even on MonoGame/KNI/FNA. An authored AnimationFrameColorOperation.Add frame
        // with Red/Green/Blue=255 renders as a flat white silhouette. Left plays the chain normally
        // (no color op authored, so it looks like the plain frame); right's chain frame carries the
        // Add tint. Mirrors the raylib/SilkNetGum NineSliceScreen's identical row.
        AddLabel(container, "Add color operation (normal frame, white-silhouette Add frame):");
        var addRow = AddRow(container);
        var squareFrameTexture = RenderingLibrary.Content.LoaderManager.Self.LoadContent<Microsoft.Xna.Framework.Graphics.Texture2D>("SquareFrame.png");
        foreach (var addTint in new Microsoft.Xna.Framework.Color?[] { null, Color.White })
        {
            var ns = new NineSliceRuntime();
            ns.Width = 56;
            ns.Height = 56;

            var chain = new AnimationChain { Name = "AddDemo" };
            var frame = new AnimationFrame { FrameLength = 1.0f, Texture = squareFrameTexture };
            if (addTint.HasValue)
            {
                frame.ColorOperation = AnimationFrameColorOperation.Add;
                frame.Red = addTint.Value.R;
                frame.Green = addTint.Value.G;
                frame.Blue = addTint.Value.B;
            }
            chain.Add(frame);

            var chainList = new AnimationChainList();
            chainList.Add(chain);
            ns.AnimationChains = chainList;
            ns.CurrentChainName = "AddDemo";

            addRow.AddChild(ns);
        }

        // IsTilingMiddleSections: stretched (default) vs tiled.
        AddLabel(container, "IsTilingMiddleSections (left: stretched, right: tiled):");
        var tilingRow = AddRow(container);
        var stretched = new NineSliceRuntime();
        stretched.SourceFileName = "TilingFrame.png";
        stretched.Width = 220;
        stretched.Height = 56;
        tilingRow.AddChild(stretched);
        var tiled = new NineSliceRuntime();
        tiled.SourceFileName = "TilingFrame.png";
        tiled.Width = 220;
        tiled.Height = 56;
        tiled.IsTilingMiddleSections = true;
        tilingRow.AddChild(tiled);

        // CustomFrameTextureCoordinateWidth: explicit edge thickness (texture pixels) instead
        // of the default 1/3 split. SquareFrame.png is 64x64, so the default corner is ~21px.
        AddLabel(container, "CustomFrameTextureCoordinateWidth (default 1/3, 8px, 24px):");
        var customWidthRow = AddRow(container);
        foreach (var frameWidth in new float?[] { null, 8f, 24f })
        {
            var ns = new NineSliceRuntime();
            ns.SourceFileName = "SquareFrame.png";
            ns.Width = 100;
            ns.Height = 100;
            ns.CustomFrameTextureCoordinateWidth = frameWidth;
            customWidthRow.AddChild(ns);
        }

        // BorderScale combined with rotation: same source rotated 25 degrees with
        // BorderScale 1 (left) and BorderScale 8 (right) so border growth is obvious.
        AddLabel(container, "Rotated (25 deg) with BorderScale 1 and 8:");
        var borderRotRow = AddRow(container);
        borderRotRow.StackSpacing = 60;
        borderRotRow.Height = 180;
        borderRotRow.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        var rotScale1 = new NineSliceRuntime();
        rotScale1.SourceFileName = "Frame.png";
        rotScale1.Width = 120;
        rotScale1.Height = 80;
        rotScale1.BorderScale = 1f;
        rotScale1.Rotation = 25f;
        rotScale1.Y = 50;
        borderRotRow.AddChild(rotScale1);

        var rotScale8 = new NineSliceRuntime();
        rotScale8.SourceFileName = "Frame.png";
        rotScale8.Width = 120;
        rotScale8.Height = 80;
        rotScale8.BorderScale = 8f;
        rotScale8.Rotation = 25f;
        rotScale8.Y = 50f;
        borderRotRow.AddChild(rotScale8);

        // AnimationChain-driven nine-slice. The .achx is loaded from disk via
        // AnimationChainListSave.FromFile + ToAnimationChainList — the same
        // pipeline a .gumx-loaded project uses internally. Assigning the
        // resulting AnimationChainList + CurrentChainName + Animate=true drives
        // the shared AnimationChainLogic, which swaps the texture across all 9
        // internal slices on every frame change.
        AddLabel(container, "AnimationChain-driven nine-slice (Kenney pixel-adventure AnimatedFrame1.achx):");
        var animated = new NineSliceRuntime();
        animated.Width = 160;
        animated.Height = 64;
        animated.AnimationChains = LoadAnimatedFrameChain();
        animated.CurrentChainName = "Animation1";
        animated.Animate = true;
        container.AddChild(animated);
    }

    private static AnimationChainList LoadAnimatedFrameChain()
    {
        // Path is relative to FileManager.RelativeDirectory (set to "Content/" by the
        // sample's initialization), matching the convention used by SourceFileName
        // elsewhere on this screen. Inside ToAnimationChainList, FileRelativeTextures
        // resolves the per-frame texture references (tile_0064.png / tile_0065.png)
        // from the same directory as the .achx.
        AnimationChainListSave save = AnimationChainListSave.FromFile("AnimatedFrame1.achx");
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
        container.AddChild(label);
    }

    private static ContainerRuntime AddRow(ContainerRuntime container)
    {
        var row = new ContainerRuntime();
        row.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 6;
        row.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        // Width/Height = 0 + RelativeToChildren → exactly fit children. A non-zero
        // value here would be added on top of the children-extent, producing extra
        // padding the layout almost never wants.
        row.Width = 0;
        row.Height = 0;
        container.AddChild(row);
        return row;
    }
}
