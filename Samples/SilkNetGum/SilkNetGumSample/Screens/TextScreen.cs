using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SilkNetGum.Screens;

// Mirror of Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/TextScreen.cs where the backend
// supports the same surface (#3414). SkiaGum dynamic fonts do not use KernSmith, so baked text
// drop shadow (TextRuntime.HasDropshadow) is not rendered here — the rows below document that gap
// explicitly. The standalone renderable drop shadow added in #3674 is a separate, working path on
// SkiaGum; the appended AddStandaloneSkiaEffectsSection (no MonoGame mirror) exercises it.
//
// Section order in this file must match Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/TextScreen.cs
// exactly, top to bottom - before adding, removing, or reordering ANY section, check that file for
// the same change or the side-by-side comparison breaks silently. (Broke once when MonoGameGumInCode
// carried extra AddCustomOutlineText rows raylib never had - #3496.) That file is itself shared with
// (linked into) Samples/raylib/GumTest.csproj as of #3640, so it covers both those backends at once.
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

        // Skia can't bake a KernSmith shadow atlas, so the baked-drop-shadow rows the MonoGame screen
        // shows here have no Skia equivalent and are omitted rather than shown as "(not on Skia)"
        // filler. BBCode inline styling (#3679) is the Skia-supported feature that takes this
        // near-the-top slot instead, matching where the MonoGame screen shows its BBCode rows.
        AddBbCodeSection(container);

        // Skia-only standalone text effects set directly on the renderable (issue #3674 drop shadow,
        // issue #3675 outline). Unlike the baked-shadow rows above (TextRuntime.HasDropshadow, the
        // MonoGame/KNI/Raylib font-atlas path that no-ops on Skia), these render on SkiaGum. This
        // section has no MonoGameGumInCode mirror — it exercises the SkiaGum.Text renderable directly.
        AddStandaloneSkiaEffectsSection(container);

        AddOverflowSection(container);

        AddMaxLettersToShowSection(container);
    }

    // BBCode inline styling on the SkiaGum.Text renderable (#3679): a single Text whose markup mixes
    // per-run color, font size, font scale, bold, and italic. SkiaGum parses the tags and feeds
    // RichTextKit one Style per run (Text.GetStyledRuns), matching the MonoGame / Raylib inline-styling
    // path. Font = "Arial" because text can silently no-op without a font. No MonoGameGumInCode mirror —
    // this exercises the SkiaGum inline-styling path directly.
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
    // silently no-op without a font. No MonoGameGumInCode mirror — exercises the renderable directly.
    private static void AddMaxLettersToShowSection(ContainerRuntime container)
    {
        AddSectionLabel(container,
            "MaxLettersToShow (#3678): identical text, three fixed reveal counts (no timing) - full, 10, 30:");

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
    // vertical TruncateLine vs SpillOver for the same text in the same fixed-size box. Each Text sets
    // Font = "Arial" (text can silently no-op without a font). Both properties live on the renderable
    // (SkiaGum.Text) and are honored by RichTextKit's TextBlock (MaxHeight / EllipsisEnabled) in
    // Text.GetTextBlock. No MonoGameGumInCode mirror — this exercises the SkiaGum.Text renderable directly.
    private static void AddOverflowSection(ContainerRuntime container)
    {
        AddSectionLabel(container,
            "Overflow (#3677): ellipsis on horizontal overflow, then vertical TruncateLine vs SpillOver (same text + box):");

        const string longLine =
            "This is a single long line of text that will not fit within the fixed width of its box";
        const string longParagraph =
            "This is a longer block of text with enough words to wrap across several lines so it overflows " +
            "the fixed height of its box and demonstrates the difference between truncation and spillover.";

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
        TextRuntime truncateText = MakeBoxFillingText(longParagraph);
        SkiaGum.Text truncateRenderable = (SkiaGum.Text)truncateText.RenderableComponent;
        truncateRenderable.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        truncateRenderable.IsTruncatingWithEllipsisOnLastLine = true;
        truncateBox.Children.Add(truncateText);
        container.Children.Add(truncateBox);

        // (c) Vertical SpillOver (today's default): the same paragraph in the same box renders every
        // line, overflowing past the box's bottom edge.
        RectangleRuntime spillBox = MakeOverflowBox(width: 300, height: 60);
        TextRuntime spillText = MakeBoxFillingText(longParagraph);
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
        AddSectionLabel(container,
            "Standalone Skia text effects set on the renderable (#3674 drop shadow, #3675 outline, #3676 blend):");

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

        // White text with a soft black drop shadow offset down-right (#3674). The standalone shadow
        // is a canvas/ImageFilter effect on SkiaGum.Text reached via RenderableComponent — distinct
        // from TextRuntime.HasDropshadow (the baked-atlas path above), which no-ops on Skia.
        TextRuntime shadowed = new TextRuntime();
        shadowed.Text = "Drop shadow";
        shadowed.Font = "Arial";
        shadowed.FontSize = 48;
        shadowed.Red = 255;
        shadowed.Green = 255;
        shadowed.Blue = 255;

        // Deliberately bold/high-contrast so the effect is unmistakable while verifying it renders at
        // all: magenta shadow, large offset. (A subtle 3px black shadow was hard to see against the
        // cornflower-blue background behind white text.)
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

    private static void AddSectionLabel(ContainerRuntime container, string text)
    {
        TextRuntime label = new TextRuntime();
        label.Text = text;
        label.FontSize = 14;
        container.Children.Add(label);
    }
}
