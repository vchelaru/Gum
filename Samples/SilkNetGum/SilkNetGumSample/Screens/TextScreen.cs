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

        AddStandaloneSkiaEffectsSection(container);

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

    // Overflow modes on the SkiaGum.Text renderable (#3677): ellipsis on horizontal overflow, then
    // vertical TruncateLine vs SpillOver for the same-size box. Each box's text names its own mode.
    // Both properties live on the renderable (SkiaGum.Text) and are honored by RichTextKit's TextBlock
    // (MaxHeight / EllipsisEnabled) in Text.GetTextBlock.
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
        RectangleRuntime ellipsisBox = MakeOverflowBox(width: 300, height: 30);
        TextRuntime ellipsisText = MakeBoxFillingText(longLine);
        SkiaGum.Text ellipsisRenderable = (SkiaGum.Text)ellipsisText.RenderableComponent;
        ellipsisRenderable.MaxNumberOfLines = 1;
        ellipsisRenderable.IsTruncatingWithEllipsisOnLastLine = true;
        ellipsisBox.Children.Add(ellipsisText);
        container.Children.Add(ellipsisBox);

        // (b) Vertical TruncateLine: the paragraph is clipped to the lines that fit the box Height,
        // with an ellipsis on the last visible line.
        RectangleRuntime truncateBox = MakeOverflowBox(width: 300, height: 60);
        TextRuntime truncateText = MakeBoxFillingText(truncateParagraph);
        SkiaGum.Text truncateRenderable = (SkiaGum.Text)truncateText.RenderableComponent;
        truncateRenderable.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        truncateRenderable.IsTruncatingWithEllipsisOnLastLine = true;
        truncateBox.Children.Add(truncateText);
        container.Children.Add(truncateBox);

        // (c) Vertical SpillOver (today's default): the same-size box renders every line, overflowing
        // past the box's bottom edge.
        RectangleRuntime spillBox = MakeOverflowBox(width: 300, height: 60);
        TextRuntime spillText = MakeBoxFillingText(spillParagraph);
        SkiaGum.Text spillRenderable = (SkiaGum.Text)spillText.RenderableComponent;
        spillRenderable.TextOverflowVerticalMode = TextOverflowVerticalMode.SpillOver;
        spillBox.Children.Add(spillText);
        container.Children.Add(spillBox);
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

    private static void AddStandaloneSkiaEffectsSection(ContainerRuntime container)
    {
        // Yellow text with a black outline via RichTextKit's halo (#3675), next to plain yellow text.
        // Font must be set: OutlineThickness only propagates to the renderable via UpdateToFontValues
        // when the runtime's Font is non-empty (#3675), so without this the halo would silently no-op.
        TextRuntime outlined = new TextRuntime();
        outlined.Text = "Outlined";
        outlined.Font = "Arial";
        outlined.FontSize = 48;
        outlined.Red = 255;
        outlined.Green = 255;
        outlined.Blue = 0;
        outlined.OutlineThickness = 3;
        container.Children.Add(outlined);

        TextRuntime noOutline = new TextRuntime();
        noOutline.Text = "No outline";
        noOutline.Font = "Arial";
        noOutline.FontSize = 48;
        noOutline.Red = 255;
        noOutline.Green = 255;
        noOutline.Blue = 0;
        container.Children.Add(noOutline);

        // White text with a drop shadow offset down-right (#3674). The standalone shadow is a
        // canvas/ImageFilter effect on SkiaGum.Text reached via RenderableComponent — distinct from
        // TextRuntime.HasDropshadow (the baked-atlas path), which no-ops on Skia. Deliberately
        // high-contrast (magenta, large offset) so it's unmistakable against the cornflower-blue
        // background behind the white text.
        TextRuntime shadowed = new TextRuntime();
        shadowed.Text = "Drop shadow";
        shadowed.Font = "Arial";
        shadowed.FontSize = 48;
        shadowed.Red = 255;
        shadowed.Green = 255;
        shadowed.Blue = 255;

        SkiaGum.Text shadowedRenderable = (SkiaGum.Text)shadowed.RenderableComponent;
        shadowedRenderable.HasDropshadow = true;
        shadowedRenderable.DropshadowOffsetX = 8;
        shadowedRenderable.DropshadowOffsetY = 8;
        shadowedRenderable.DropshadowBlurX = 4;
        shadowedRenderable.DropshadowBlurY = 4;
        shadowedRenderable.DropshadowColor = new SKColor(255, 0, 255, 255);
        container.Children.Add(shadowed);

        // Blend on the renderable (#3676): the same warm text over a blue background renders far
        // brighter with Additive blend (left) than with the default alpha blend (right), because
        // Additive adds the text color to the background instead of covering it. Blend is applied as
        // an SKPaint.BlendMode in Text.Render (see Text.GetRenderPaint). Font must be set or the text
        // can silently no-op.
        RectangleRuntime blendBackground = new RectangleRuntime();
        blendBackground.Width = 520;
        blendBackground.Height = 60;
        blendBackground.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        blendBackground.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        blendBackground.FillColor = new SKColor(40, 40, 120);
        blendBackground.IsFilled = true;

        TextRuntime additiveBlend = new TextRuntime();
        additiveBlend.Text = "Additive";
        additiveBlend.Font = "Arial";
        additiveBlend.FontSize = 36;
        additiveBlend.Red = 210;
        additiveBlend.Green = 150;
        additiveBlend.Blue = 40;
        additiveBlend.X = 8;
        additiveBlend.Y = 8;
        ((SkiaGum.Text)additiveBlend.RenderableComponent).Blend = Gum.RenderingLibrary.Blend.Additive;
        blendBackground.Children.Add(additiveBlend);

        TextRuntime normalBlend = new TextRuntime();
        normalBlend.Text = "Normal";
        normalBlend.Font = "Arial";
        normalBlend.FontSize = 36;
        normalBlend.Red = 210;
        normalBlend.Green = 150;
        normalBlend.Blue = 40;
        normalBlend.X = 300;
        normalBlend.Y = 8;
        blendBackground.Children.Add(normalBlend);

        container.Children.Add(blendBackground);
    }
}
