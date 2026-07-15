using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;
using System;
using System.Linq;

namespace SilkNetGum.Screens;

// Companion to Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/TextScreen.cs (#3414). By policy
// the three text samples are kept feature-, content-, AND section-order-identical (a new section goes
// in the SAME position in both files, not just present) and carry NO descriptive section labels —
// each demo's own text shows AND names what it is. This SilkNetGum screen is still a
// separate, non-linked file for now (a later step folds all three into one shared source). Sections
// that are genuinely Skia-specific — standalone renderable outline / drop shadow / blend
// (#3674/#3675/#3676), RichTextKit overflow (#3677), and MaxLettersToShow (#3678) — exercise the
// SkiaGum.Text renderable directly; the baked-atlas drop shadow and texture-filter demos the MonoGame
// screen shows have no Skia equivalent and are intentionally absent.
//
// Tick(elapsedSeconds) (#3701) drives the animated typewriter section and must be called once per
// frame by Program.cs's main loop while this screen is active (`(currentCodeScreen as
// TextScreen)?.Tick(...)`). FrameworkElement.Activity() is FRB-only (`#if FRB`) and not available in
// these plain samples, hence the host-driven Tick instead.
internal class TextScreen : FrameworkElement
{
    public TextScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime container = new ContainerRuntime();
        container.WidthUnits = DimensionUnitType.RelativeToParent;
        container.HeightUnits = DimensionUnitType.RelativeToParent;
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

        // Placed right after the default text -- this screen has no ScrollViewer, and the rest of the
        // stack (BBCode/shadows/blend/overflow/static reveal rows) comfortably exceeds a typical window
        // height, so anything appended after it is invisible without resizing.
        AddTypewriterSection(container);

        AddBbCodeSection(container);

        // Wavy rainbow text: the same [Custom=Wave] per-letter callback the MonoGame/raylib screen uses
        // (issue #3692) -- SkiaGum resolves it through its own Text.Customizations registry (SkiaGum
        // cannot share MonoGame's, since that lives in the MonoGame-coupled RenderingLibrary.Graphics.Text
        // source file), applying YOffset as a post-layout glyph nudge and Color as a normal per-run style.
        SkiaGum.Text.Customizations["Wave"] = (int index, string block) => new SkiaGum.LetterCustomization
        {
            YOffset = MathF.Sin(index * 0.9f) * 10f,
            Color = System.Drawing.Color.FromArgb(
                255,
                (int)(128 + 127 * MathF.Sin(index * 0.7f)),
                (int)(128 + 127 * MathF.Sin(index * 0.7f + 2f)),
                (int)(128 + 127 * MathF.Sin(index * 0.7f + 4f))),
        };
        TextRuntime wavy = new TextRuntime();
        wavy.Font = "Arial";
        wavy.FontSize = 24;
        wavy.Text = "[Custom=Wave]Wavy rainbow text[/Custom]";
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

        // #3670/#3703: CustomFontFile pointing at a bundled .ttf -- Skia previously had no
        // custom-font-file loading path at all, so this silently fell back to the default font.
        // Path is relative to FileManager.RelativeDirectory, which this sample's GumUI.Initialize
        // (loading Content/GumProject/GumProject.gumx) points at the project folder, not the exe
        // root -- so the file lives at Content/GumProject/Fonts/CustomFont.ttf on disk.
        TextRuntime customFontFileText = new TextRuntime();
        customFontFileText.Text = "I use a bundled .ttf via UseCustomFont + CustomFontFile";
        customFontFileText.FontSize = 24;
        customFontFileText.UseCustomFont = true;
        customFontFileText.CustomFontFile = "Fonts/CustomFont.ttf";
        container.Children.Add(customFontFileText);

        AddBlendOnTextSection(container);

        AddOverflowSection(container);

        // GetCharacterIndexAtPosition (#3708): click the text, report the hit index. Newly implemented
        // on SkiaGum.Text via RichTextKit's TextBlock.HitTest -- same position in this section as the
        // MonoGame/raylib screen's BuildTextParitySection, minus the TextRenderingPositionMode toggle
        // right before it there, which is still a Skia gap (#3708) and has no equivalent here.
        TextRuntime hitText = new TextRuntime();
        hitText.FontSize = 24;
        hitText.Text = "Click me to report the character index";
        hitText.HasEvents = true;
        container.Children.Add(hitText);

        TextRuntime hitResult = new TextRuntime();
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

        AddMaxLettersToShowSection(container);
    }

    // Typewriter reveal (#3701): MaxLettersToShow (the same property AddRevealRow sets to a
    // fixed count) driven by elapsed time instead, via Tick. Pressing any key restarts the reveal
    // from the beginning. FormsUtilities.Keyboard.KeysTyped is the platform-neutral "keys typed this
    // frame" feed every backend's UseKeyboardDefaults() populates, so this needs no #if branching.
    private const string TypewriterParagraph =
        "This paragraph types itself out one letter at a time. Press any key to start the reveal over from the beginning.";
    private const double TypewriterLettersPerSecond = 18;
    private TextRuntime _typewriterText;
    private double _typewriterElapsedSeconds;

    /// <summary>
    /// Advances the typewriter reveal by <paramref name="elapsedSeconds"/>. Call once per frame from
    /// the host's game loop while this screen is active.
    /// </summary>
    public void Tick(double elapsedSeconds)
    {
        if (FormsUtilities.Keyboard.KeysTyped.Any())
        {
            _typewriterElapsedSeconds = 0;
        }
        else
        {
            _typewriterElapsedSeconds += elapsedSeconds;
        }

        int lettersToShow = (int)(_typewriterElapsedSeconds * TypewriterLettersPerSecond);
        // TextRuntime.MaxLettersToShow is #if !SKIA-gated (see AddRevealRow above), so this goes
        // through the renderable directly like the other MaxLettersToShow rows do.
        ((SkiaGum.Text)_typewriterText.RenderableComponent).MaxLettersToShow =
            Math.Min(lettersToShow, TypewriterParagraph.Length);
    }

    private void AddTypewriterSection(ContainerRuntime container)
    {
        _typewriterText = new TextRuntime();
        _typewriterText.Font = "Arial";
        _typewriterText.FontSize = 20;
        _typewriterText.WidthUnits = DimensionUnitType.Absolute;
        _typewriterText.Width = 300;
        _typewriterText.HeightUnits = DimensionUnitType.RelativeToChildren;
        _typewriterText.Height = 0;
        _typewriterText.Text = TypewriterParagraph;
        ((SkiaGum.Text)_typewriterText.RenderableComponent).MaxLettersToShow = 0;
        container.Children.Add(_typewriterText);
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
        bbcode.WidthUnits = DimensionUnitType.Absolute;
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
        row.WidthUnits = DimensionUnitType.Absolute;
        row.Width = 300;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Height = 0;
        row.Text = paragraph;
        // TextRuntime.MaxLettersToShow is #if !SKIA-gated in MonoGameGum/GueDeriving/TextRuntime.cs --
        // SkiaGum.Text supports the property, the shared runtime forwarder just hasn't caught up -- so
        // this goes through the renderable directly instead of the (MonoGame/raylib-only) runtime property.
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
        spillSpacer.WidthUnits = DimensionUnitType.Absolute;
        spillSpacer.HeightUnits = DimensionUnitType.Absolute;
        spillSpacer.Width = 0;
        spillSpacer.Height = 40;
        container.Children.Add(spillSpacer);
    }

    // A fixed-size, translucent-bordered box so the text's overflow bounds are visible.
    private static RectangleRuntime MakeOverflowBox(float width, float height)
    {
        RectangleRuntime box = new RectangleRuntime();
        box.WidthUnits = DimensionUnitType.Absolute;
        box.HeightUnits = DimensionUnitType.Absolute;
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
        textRuntime.WidthUnits = DimensionUnitType.RelativeToParent;
        textRuntime.HeightUnits = DimensionUnitType.RelativeToParent;
        textRuntime.Width = 0;
        textRuntime.Height = 0;
        return textRuntime;
    }

    // Blend on Text (#3432): additive (brightens) vs normal, over an identical blue box. Each cell's
    // own text ("Additive" / "Normal") names its blend mode.
    private static void AddBlendOnTextSection(ContainerRuntime container)
    {
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
        background.FillColor = new SKColor(40, 60, 160, 255);
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
}
