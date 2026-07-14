using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SilkNetGum.Screens;

// Companion to Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/TextScreen.cs (#3414). By policy
// the three text samples are kept feature- and content-identical and carry NO descriptive section
// labels — each demo's own text shows AND names what it is. This SilkNetGum screen is still a
// separate, non-linked file for now (a later step folds all three into one shared source). Sections
// that are genuinely Skia-specific — standalone renderable outline / drop shadow / blend
// (#3674/#3675/#3676), RichTextKit overflow (#3677), and MaxLettersToShow (#3678) — exercise the
// SkiaGum.Text renderable directly; the baked-atlas drop shadow and texture-filter demos the MonoGame
// screen shows have no Skia equivalent and are intentionally absent.
internal class TextScreen : FrameworkElement
{
    public TextScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime container = new ContainerRuntime();
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.X = 2;
        container.Y = 2;
        container.Width = -4;
        container.Height = -4;
        container.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        container.StackSpacing = 4;
        this.AddChild(container);

        TextRuntime textRuntime = new TextRuntime();
        textRuntime.Text = "Hi, I'm default text";
        container.Children.Add(textRuntime);

        AddBbCodeSection(container);

        // Wavy rainbow text: the [Custom] per-letter callback the MonoGame screen uses for this is
        // intentionally not implemented on SkiaGum yet (deferred scope, see Text.SupportedTags), so this
        // is a plain-text placeholder for parity until it lands.
        TextRuntime wavy = new TextRuntime();
        wavy.Font = "Arial";
        wavy.FontSize = 24;
        wavy.Text = "Wavy rainbow text";
        container.Children.Add(wavy);

        // Drop shadow (TextRuntime.HasDropshadow) — the cross-backend shadow API. MonoGame/KNI/Raylib
        // bake it into the font atlas via KernSmith; on SkiaGum (no atlas) SystemManagers.UpdateFonts
        // maps it onto the renderable's standalone ImageFilter shadow, so the same API renders here too.
        // OutlineThickness (#3675) renders via RichTextKit's halo. Both work without an explicit Font.
        TextRuntime shadowDefault = new TextRuntime();
        shadowDefault.Text = "Soft shadow";
        shadowDefault.FontSize = 24;
        shadowDefault.HasDropshadow = true;
        container.Children.Add(shadowDefault);

        TextRuntime shadowColored = new TextRuntime();
        shadowColored.Text = "Pink shadow, offset, and blurred";
        shadowColored.FontSize = 24;
        shadowColored.HasDropshadow = true;
        shadowColored.DropshadowColor = new SKColor(220, 40, 160, 220);
        shadowColored.DropshadowOffsetX = 2;
        shadowColored.DropshadowOffsetY = 4;
        shadowColored.DropshadowBlur = 4;
        container.Children.Add(shadowColored);

        TextRuntime shadowOutline = new TextRuntime();
        shadowOutline.Text = "Shadow and outline";
        shadowOutline.FontSize = 24;
        shadowOutline.OutlineThickness = 2;
        shadowOutline.HasDropshadow = true;
        container.Children.Add(shadowOutline);

        TextRuntime withOutline = new TextRuntime();
        withOutline.Text = "I am text with OutlineThickness = 2";
        withOutline.FontSize = 24;
        withOutline.OutlineThickness = 2;
        container.Children.Add(withOutline);

        AddBlendOnTextSection(container);

        AddOverflowSection(container);

        AddMaxLettersToShowSection(container);
    }

    // BBCode inline styling on the SkiaGum.Text renderable (#3679): a single Text whose markup mixes
    // per-run color, font size, font scale, bold, and italic. SkiaGum parses the tags and feeds
    // RichTextKit one Style per run (Text.GetStyledRuns), matching the MonoGame / Raylib inline-styling
    // path. Font = "Arial" because text can silently no-op without a font.
    private static void AddBbCodeSection(ContainerRuntime container)
    {
        // One self-describing BBCode line: each styled word shows AND names its own effect, so no
        // separate label is needed. Kept byte-identical to the same line in the MonoGameGumInCode text
        // screen — the three text samples are feature- and content-identical by policy.
        TextRuntime bbcode = new TextRuntime();
        bbcode.Font = "Arial";
        bbcode.FontSize = 24;
        bbcode.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        bbcode.Width = 520;
        bbcode.Text =
            "[Color=Red]red[/Color], [Color=Blue]blue[/Color], " +
            "[FontSize=40]big[/FontSize], [FontScale=1.5]scaled[/FontScale], " +
            "[IsBold=true]bold[/IsBold], and [IsItalic=true]italic[/IsItalic] runs, all styled inline in one Text.";
        container.Children.Add(bbcode);
    }

    // MaxLettersToShow typewriter reveal on the SkiaGum.Text renderable (#3678). The same wrapping
    // paragraph is shown fully, then with MaxLettersToShow set to a partial count so only the first N
    // letters are visible while the hidden tail still occupies its final layout (reveal is paint-only:
    // WrappedText / measurement stay built from the full RawText). Font = "Arial" because text can
    // silently no-op without a font.
    private static void AddMaxLettersToShowSection(ContainerRuntime container)
    {
        const string paragraph =
            "This paragraph reveals only its first letters while the rest stays hidden.";

        // Explicit RelativeToChildren height so each Text takes its own content height in the stack
        // (leaving Height at its default let the rows overlap in the earlier demo). null MaxLettersToShow
        // = full text; 10 and 30 are fixed counts so the difference is visible without any animation.
        AddRevealRow(container, paragraph, null);
        AddRevealRow(container, paragraph, 10);
        AddRevealRow(container, paragraph, 30);
    }

    private static void AddRevealRow(ContainerRuntime container, string paragraph, int? maxLetters)
    {
        TextRuntime row = new TextRuntime();
        row.Font = "Arial";
        row.FontSize = 20;
        row.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        row.Width = 300;
        row.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.Height = 0;
        row.Text = paragraph;
        ((SkiaGum.Text)row.RenderableComponent).MaxLettersToShow = maxLetters;
        container.Children.Add(row);
    }

    // Overflow modes (#3677): horizontal ellipsis, then vertical TruncateLine vs SpillOver for the
    // same-size box. Each box's text names its own mode. The overflow properties live on TextRuntime
    // (MaxNumberOfLines / IsTruncatingWithEllipsisOnLastLine / TextOverflowVerticalMode), so no cast to
    // the renderable is needed. Kept content-identical to the MonoGameGumInCode text screen.
    private static void AddOverflowSection(ContainerRuntime container)
    {
        const string longLine =
            "This single long line overflows its box horizontally and is cut off with an ellipsis";
        const string truncateParagraph =
            "TruncateLine clips this wrapping paragraph to the lines that fit the box height and " +
            "ellipsizes the last visible line, dropping everything past it.";
        const string spillParagraph =
            "SpillOver renders every line of this wrapping paragraph, overflowing past the bottom " +
            "edge of its box instead of clipping.";

        // (a) Horizontal overflow -> ellipsis. MaxNumberOfLines = 1 caps the block to one line;
        // IsTruncatingWithEllipsisOnLastLine appends the trailing "...".
        var ellipsisBox = MakeOverflowBox(width: 300, height: 30);
        var ellipsisText = MakeBoxFillingText(longLine);
        ellipsisText.MaxNumberOfLines = 1;
        ellipsisText.IsTruncatingWithEllipsisOnLastLine = true;
        ellipsisBox.Children.Add(ellipsisText);
        container.Children.Add(ellipsisBox);

        // (b) Vertical TruncateLine: the paragraph is clipped to the lines that fit the box Height,
        // with an ellipsis on the last visible line.
        var truncateBox = MakeOverflowBox(width: 300, height: 60);
        var truncateText = MakeBoxFillingText(truncateParagraph);
        truncateText.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        truncateText.IsTruncatingWithEllipsisOnLastLine = true;
        truncateBox.Children.Add(truncateText);
        container.Children.Add(truncateBox);

        // (c) Vertical SpillOver: the same-size box renders every line, overflowing past the box's
        // bottom edge.
        var spillBox = MakeOverflowBox(width: 300, height: 60);
        var spillText = MakeBoxFillingText(spillParagraph);
        spillText.TextOverflowVerticalMode = TextOverflowVerticalMode.SpillOver;
        spillBox.Children.Add(spillText);
        container.Children.Add(spillBox);

        // The SpillOver box renders past its own bottom edge by design; reserve room below it so the
        // overflowing lines don't land on top of the next section in the top-to-bottom stack.
        var spillSpacer = new ContainerRuntime();
        spillSpacer.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        spillSpacer.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        spillSpacer.Width = 0;
        spillSpacer.Height = 40;
        container.Children.Add(spillSpacer);
    }

    // A fixed-size, translucent-bordered box so the text's overflow bounds are visible.
    private static RectangleRuntime MakeOverflowBox(float width, float height)
    {
        RectangleRuntime box = new RectangleRuntime();
        box.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        box.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        box.Width = width;
        box.Height = height;
        box.FillColor = new SKColor(40, 40, 40);
        box.IsFilled = true;
        return box;
    }

    // A Text that fills its parent box, so its overflow is exactly the box's bounds.
    private static TextRuntime MakeBoxFillingText(string text)
    {
        TextRuntime textRuntime = new TextRuntime();
        textRuntime.Text = text;
        textRuntime.Font = "Arial";
        textRuntime.FontSize = 18;
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        textRuntime.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        textRuntime.Width = 0;
        textRuntime.Height = 0;
        return textRuntime;
    }

    // Blend on Text (#3432): additive (brightens) vs normal, over an identical blue box. Each cell's
    // own text ("Additive" / "Normal") names its blend mode.
    private static void AddBlendOnTextSection(ContainerRuntime container)
    {
        var blendRow = new ContainerRuntime();
        blendRow.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        blendRow.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
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
        background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        background.Width = 0;
        background.Height = 0;
        background.IsFilled = true;
        background.FillColor = new SKColor(40, 60, 160, 255);
        cell.Children.Add(background);

        var text = new TextRuntime();
        text.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        text.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
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
}
