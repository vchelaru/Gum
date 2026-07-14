using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
#if RAYLIB
using Color = Raylib_cs.Color;
using Text = Gum.Renderables.Text;
using LetterCustomization = Gum.Renderables.LetterCustomization;
using TextRenderingPositionMode = Gum.Renderables.TextRenderingPositionMode;
#else
using Color = Microsoft.Xna.Framework.Color;
using Text = RenderingLibrary.Graphics.Text;
using LetterCustomization = RenderingLibrary.Graphics.LetterCustomization;
using TextRenderingPositionMode = RenderingLibrary.Graphics.TextRenderingPositionMode;
#endif

#if RAYLIB
namespace Examples.Shapes;
#else
namespace MonoGameGumInCode.Screens;
#endif

// #3640: converged into a single shared file (was two byte-for-byte-mirrored copies at
// Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/TextScreen.cs and
// Samples/raylib/Screens/TextScreen.cs) after repeatedly drifting when only one mirror got updated.
// Linked into Samples/raylib/GumTest.csproj via <Compile Include ... Link>; this file is the only
// copy. The SilkNetGum TextScreen.cs mirror (Samples/SilkNetGum/SilkNetGum/Screens/TextScreen.cs)
// stays a separate, non-linked file - it legitimately omits the BBCode / Blend / TextureFilter
// sections because SkiaGum renders via RichTextKit rather than a font atlas, so there's much less
// left to share. Only genuinely backend-specific things differ here, gated `#if RAYLIB`: the
// namespace, the Color/Text/LetterCustomization/TextRenderingPositionMode aliases above, and
// AddTextureFilterSection's mechanism (a per-layer sampler state on MonoGame vs a baked
// font-cache texture on raylib).
internal class TextScreen : FrameworkElement
{
    public TextScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var container = new ContainerRuntime();
        container.WidthUnits = DimensionUnitType.RelativeToParent;
        container.HeightUnits = DimensionUnitType.RelativeToParent;
        container.X = 2;
        container.Y = 2;
        container.Width = -4;
        container.Height = -4;
        container.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        container.StackSpacing = 4;
        this.AddChild(container);

        var textRuntime = new TextRuntime();
        textRuntime.Text = "Hi, I'm default text";
        container.Children.Add(textRuntime);

        // One self-describing BBCode line: each styled word shows AND names its own effect, so no
        // separate label is needed. Kept byte-identical to the same line in the SilkNetGumSample text
        // screen — the three text samples are feature- and content-identical by policy.
        var bbcode = new TextRuntime();
        bbcode.Font = "Arial";
        bbcode.FontSize = 24;
        bbcode.WidthUnits = DimensionUnitType.Absolute;
        bbcode.Width = 520;
        bbcode.Text =
            "[Color=Red]red[/Color], [Color=Blue]blue[/Color], " +
            "[FontSize=40]big[/FontSize], [FontScale=1.5]scaled[/FontScale], " +
            "[IsBold=true]bold[/IsBold], and [IsItalic=true]italic[/IsItalic] runs, all styled inline in one Text.";
        container.Children.Add(bbcode);

        AddSectionLabel(container, "BBCode markup - [FontSize=40] crisp swap (left) vs [FontScale=1.9] scale-up (right); blue box = measured width (#3524):");
        container.Children.Add(BuildFontSizeContainmentRow());

        AddSectionLabel(container, "BBCode markup - [Custom] per-letter callback (#3640): static wavy offset + per-letter hue");
        Text.Customizations["Wave"] = (int index, string block) => new LetterCustomization
        {
            YOffset = MathF.Sin(index * 0.9f) * 10f,
            Color = System.Drawing.Color.FromArgb(
                255,
                (int)(128 + 127 * MathF.Sin(index * 0.7f)),
                (int)(128 + 127 * MathF.Sin(index * 0.7f + 2f)),
                (int)(128 + 127 * MathF.Sin(index * 0.7f + 4f))),
        };
        var customMarkup = new TextRuntime();
        customMarkup.FontSize = 24;
        customMarkup.Text = "[Custom=Wave]Wavy rainbow text[/Custom]";
        container.Children.Add(customMarkup);

        AddSectionLabel(container, "Baked drop shadow (HasDropshadow = true, first-enable defaults):");
        var shadowDefault = new TextRuntime();
        shadowDefault.Text = "Soft baked shadow";
        shadowDefault.FontSize = 24;
        shadowDefault.HasDropshadow = true;
        container.Children.Add(shadowDefault);

        AddSectionLabel(container, "Baked drop shadow (colored, offset, blur):");
        var shadowColored = new TextRuntime();
        shadowColored.Text = "Pink shadow";
        shadowColored.FontSize = 24;
        shadowColored.HasDropshadow = true;
        shadowColored.DropshadowColor = new Color(220, 40, 160, 220);
        shadowColored.DropshadowOffsetX = 2;
        shadowColored.DropshadowOffsetY = 4;
        shadowColored.DropshadowBlur = 4;
        container.Children.Add(shadowColored);

        AddSectionLabel(container, "Baked drop shadow + outline:");
        var shadowOutline = new TextRuntime();
        shadowOutline.Text = "Shadow and outline";
        shadowOutline.FontSize = 24;
        shadowOutline.OutlineThickness = 2;
        shadowOutline.HasDropshadow = true;
        container.Children.Add(shadowOutline);

        var withOutline = new TextRuntime();
        withOutline.Text = "I am text with OutlineThickness = 2";
        withOutline.FontSize = 24;
        withOutline.OutlineThickness = 2;
        container.Children.Add(withOutline);

        BuildTextParitySection(container);
    }

    // Text parity features (#3432): Blend, per-instance TextRenderingPositionMode override, and
    // GetCharacterIndexAtPosition. All three are runtime-observable, so this section is the manual
    // verification surface for the parity batch.
    private static void BuildTextParitySection(ContainerRuntime container)
    {
        AddBlendOnTextSection(container);
        AddTextureFilterSection(container);

        // --- Per-instance TextRenderingPositionMode override, at a fractional origin ---
        AddSectionLabel(container, "TextRenderingPositionMode (#3432): fractional origin; button toggles snap");
        var snapText = new TextRuntime();
        snapText.FontSize = 20;
        snapText.X = 120.5f;
        snapText.Text = "Fractional X=120.5 - SnapToPixel";
        snapText.TextRenderingPositionMode = TextRenderingPositionMode.SnapToPixel;
        container.Children.Add(snapText);

        var toggleButton = new Button();
        toggleButton.Text = "Toggle snap mode";
        toggleButton.Click += (_, _) =>
        {
            if (snapText.TextRenderingPositionMode == TextRenderingPositionMode.SnapToPixel)
            {
                snapText.TextRenderingPositionMode = TextRenderingPositionMode.FreeFloating;
                snapText.Text = "Fractional X=120.5 - FreeFloating";
            }
            else
            {
                snapText.TextRenderingPositionMode = TextRenderingPositionMode.SnapToPixel;
                snapText.Text = "Fractional X=120.5 - SnapToPixel";
            }
        };
        container.Children.Add(toggleButton.Visual);

        // --- GetCharacterIndexAtPosition: click the text, report the hit index ---
        AddSectionLabel(container, "GetCharacterIndexAtPosition (#3432): click the text below");
        var hitText = new TextRuntime();
        hitText.FontSize = 24;
        hitText.Text = "Click me to report the character index";
        hitText.HasEvents = true;
        container.Children.Add(hitText);

        var hitResult = new TextRuntime();
        hitResult.FontSize = 16;
        hitResult.Text = "(no click yet)";
        container.Children.Add(hitResult);

        hitText.Click += (_, _) =>
        {
            float cursorX = FrameworkElement.MainCursor.XRespectingGumZoomAndBounds();
            float cursorY = FrameworkElement.MainCursor.YRespectingGumZoomAndBounds();
            int index = hitText.GetCharacterIndexAtPosition(cursorX, cursorY);
            hitResult.Text = $"Character index at click: {index}";
        };
    }

    // Texture filter on Text (#3496): a font baked SMALL then magnified via FontScale, so
    // point-filtering's blocky glyph edges are visibly distinct from bilinear's smoothed ones. A
    // large FontSize at 1x scale doesn't stress the sampler enough to show a difference - the atlas
    // texel density roughly matches screen pixel density either way. Baking small and scaling up
    // means each atlas texel covers several screen pixels, which is exactly when nearest-neighbor
    // vs bilinear diverge. Renderer.TextureFilter is a single global sampler state for the whole
    // SpriteBatch pass, so one Text can't be Point and another Linear on the same layer (see
    // docs/code/rendering/texture-filtering.md) - each side gets its own Layer with
    // Layer.IsLinearFilteringEnabled forcing the mode, while layout still comes from the shared
    // filterRow container. Unlike raylib (where the filter is baked into the font-cache texture at
    // creation time), the layer-based override here is a pure render-state switch, so both sides can
    // share the same FontSize/FontScale.
    private static void AddTextureFilterSection(ContainerRuntime container)
    {
        AddSectionLabel(container, "Texture filter (#3496): 12px font scaled 4x, Point (left) vs Linear (right)");
        var filterRow = new ContainerRuntime();
        filterRow.WidthUnits = DimensionUnitType.RelativeToChildren;
        filterRow.HeightUnits = DimensionUnitType.RelativeToChildren;
        filterRow.Width = 0;
        filterRow.Height = 0;
        filterRow.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        filterRow.StackSpacing = 16;
#if RAYLIB
        var savedFilter = RenderingLibrary.Content.ContentLoader.DefaultTextureFilter;

        RenderingLibrary.Content.ContentLoader.DefaultTextureFilter = Raylib_cs.TextureFilter.Point;
        var pointText = new TextRuntime();
        pointText.FontSize = 12;
        pointText.FontScale = 4;
        pointText.Text = "Point";
        filterRow.AddChild(pointText);

        RenderingLibrary.Content.ContentLoader.DefaultTextureFilter = Raylib_cs.TextureFilter.Bilinear;
        var linearText = new TextRuntime();
        linearText.FontSize = 13; // distinct size so this is a separate font-cache entry from "Point" above
        linearText.FontScale = 4;
        linearText.Text = "Linear";
        filterRow.AddChild(linearText);

        RenderingLibrary.Content.ContentLoader.DefaultTextureFilter = savedFilter;

        container.Children.Add(filterRow);
#else
        container.Children.Add(filterRow);

        var pointLayer = SystemManagers.Default.Renderer.AddLayer();
        pointLayer.Name = "Texture Filter - Point";
        pointLayer.IsLinearFilteringEnabled = false;
        var pointText = new TextRuntime();
        pointText.FontSize = 12;
        pointText.FontScale = 4;
        pointText.Text = "Point";
        filterRow.AddChild(pointText);
        pointText.MoveToLayer(pointLayer);

        var linearLayer = SystemManagers.Default.Renderer.AddLayer();
        linearLayer.Name = "Texture Filter - Linear";
        linearLayer.IsLinearFilteringEnabled = true;
        var linearText = new TextRuntime();
        linearText.FontSize = 12;
        linearText.FontScale = 4;
        linearText.Text = "Linear";
        filterRow.AddChild(linearText);
        linearText.MoveToLayer(linearLayer);
#endif
    }

    // Blend on Text (#3432): additive (brightens) vs normal, over an identical blue box.
    private static void AddBlendOnTextSection(ContainerRuntime container)
    {
        AddSectionLabel(container, "Blend on Text (#3432): additive (brightens) vs normal, over a blue box");
        var blendRow = new ContainerRuntime();
        blendRow.WidthUnits = DimensionUnitType.RelativeToChildren;
        blendRow.HeightUnits = DimensionUnitType.RelativeToChildren;
        blendRow.Width = 0;
        blendRow.Height = 0;
        blendRow.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        blendRow.StackSpacing = 16;
        blendRow.AddChild(BuildBlendCell("Additive", Gum.RenderingLibrary.Blend.Additive));
        blendRow.AddChild(BuildBlendCell("Normal", null));
        container.Children.Add(blendRow);
    }

    private static ContainerRuntime BuildBlendCell(string label, Gum.RenderingLibrary.Blend? blend)
    {
        var cell = new ContainerRuntime();
        cell.Width = 200;
        cell.Height = 48;

        var background = new RectangleRuntime();
        background.WidthUnits = DimensionUnitType.RelativeToParent;
        background.HeightUnits = DimensionUnitType.RelativeToParent;
        background.Width = 0;
        background.Height = 0;
        background.IsFilled = true;
        background.FillColor = new Color(40, 60, 160, 255);
        cell.Children.Add(background);

        var text = new TextRuntime();
        text.WidthUnits = DimensionUnitType.RelativeToParent;
        text.HeightUnits = DimensionUnitType.RelativeToParent;
        text.Width = 0;
        text.Height = 0;
        text.FontSize = 24;
        text.Text = label;
        // Amber, not white: additive can only brighten, and white is already maxed, so white text
        // renders identically under Additive and Normal. A mid-intensity warm color visibly washes
        // out toward bright peach when added to the blue box, making the Additive cell obviously
        // different from the Normal one.
        text.Red = 230;
        text.Green = 150;
        text.Blue = 40;
        text.HorizontalAlignment = HorizontalAlignment.Center;
        text.VerticalAlignment = VerticalAlignment.Center;
        text.Blend = blend;
        cell.Children.Add(text);

        return cell;
    }

    // Inline FontSize font-swap (#3524): [FontSize=N] swaps in a font rasterized at N (crisp) and must
    // also be MEASURED at that size, or a RelativeToChildren Text is sized too narrow and the run spills
    // past its background (the RelativeToChildren-too-narrow bug fixed in #3520 / #3523). Left cell =
    // [FontSize=40] over a 21px base (crisp swap); right cell = a [FontScale=1.9] control (a scale-up of
    // the 21px atlas, so visibly blurrier). Pass = "big" is enlarged, crisper on the left than the right,
    // AND the blue box fully contains each line in both (raylib gets its crisp swap from KernSmith;
    // MonoGame from the BitmapFont path - same visual contract either way).
    private static ContainerRuntime BuildFontSizeContainmentRow()
    {
        var row = new ContainerRuntime();
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 24;
        row.AddChild(BuildContainedMarkupCell("This is [FontSize=40]big[/FontSize] text."));
        row.AddChild(BuildContainedMarkupCell("This is [FontScale=1.9]big[/FontScale] text."));
        return row;
    }

    // A RelativeToChildren cell sized to its TextRuntime, with a RelativeToParent background filling it.
    // The background's edges mark where measurement thinks the text ends, so any measure-vs-render drift
    // shows up as the run spilling outside the blue box.
    private static ContainerRuntime BuildContainedMarkupCell(string markup)
    {
        var cell = new ContainerRuntime();
        cell.WidthUnits = DimensionUnitType.RelativeToChildren;
        cell.HeightUnits = DimensionUnitType.RelativeToChildren;
        cell.Width = 0;
        cell.Height = 0;

        var background = new RectangleRuntime();
        background.WidthUnits = DimensionUnitType.RelativeToParent;
        background.HeightUnits = DimensionUnitType.RelativeToParent;
        background.Width = 0;
        background.Height = 0;
        background.IsFilled = true;
        background.FillColor = new Color(40, 60, 160, 255);
        cell.Children.Add(background);

        var text = new TextRuntime();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.Height = 0;
        text.FontSize = 21;
        text.Text = markup;
        cell.Children.Add(text);

        return cell;
    }

    private static void AddSectionLabel(ContainerRuntime container, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        label.FontSize = 14;
        container.Children.Add(label);
    }
}
