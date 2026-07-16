using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
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
// Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/TextScreen.cs and Samples/raylib/Screens/
// TextScreen.cs) after repeatedly drifting when only one mirror got updated. Linked into
// Samples/raylib/GumTest.csproj via <Compile Include ... Link>; this file is the only copy for
// MonoGame + raylib.
//
// Policy: the MonoGame/raylib and SilkNetGum text samples are kept feature-, content-, AND
// section-order-identical (a new section goes in the SAME position in both files, not just present),
// and carry NO descriptive section labels — each demo's own text shows AND names what it is. The
// SilkNetGum mirror (Samples/SilkNetGum/SilkNetGumSample/Screens/TextScreen.cs) is still a separate,
// non-linked file for now. Only genuinely backend-specific things differ here, gated `#if RAYLIB`:
// the namespace, the Color/Text/LetterCustomization/TextRenderingPositionMode aliases above, and
// AddTextureFilterSection's mechanism (a per-layer sampler state on MonoGame vs a baked font-cache
// texture on raylib).
//
// Tick(elapsedSeconds) (#3701) drives the animated typewriter section and must be called once per
// frame by the host while this screen is active — see Game1.Update / Program.cs's main loop
// (`(currentScreen as TextScreen)?.Tick(...)`). FrameworkElement.Activity() is FRB-only (`#if FRB`)
// and not available in these plain samples, hence the host-driven Tick instead.
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

        // Placed right after the default text -- this screen has no ScrollViewer, and the rest of the
        // stack (BBCode/shadows/BuildTextParitySection/static reveal rows) comfortably exceeds a
        // typical window height, so anything appended after it is invisible without resizing.
        AddTypewriterSection(container);

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

        // [State=Name] BBCode tag (MonoGame/raylib only -- SkiaGum has its own separate
        // CustomSetPropertyOnRenderable.cs without this feature yet, so there's no SilkNetGum mirror
        // for this section). The state is defined in code via AddStates (no Gum project needed) with
        // a Color + IsBold pair; only variables already wired for per-run application (like these two)
        // apply to the wrapped substring -- a state can hold anything, including layout properties like
        // X/Width, but those are silently skipped rather than applied to the whole element.
        var stateBbcode = new TextRuntime();
        stateBbcode.Font = "Arial";
        stateBbcode.FontSize = 24;
        var highlightedState = new StateSave { Name = "Highlighted" };
        highlightedState.Variables.Add(new VariableSave { Name = "Color", Value = System.Drawing.Color.Gold });
        highlightedState.Variables.Add(new VariableSave { Name = "IsBold", Value = true });
        stateBbcode.AddStates(new List<StateSave> { highlightedState });
        stateBbcode.Text = "Plain text with a [State=Highlighted]bold gold run[/State] applied from a code-defined state.";
        container.Children.Add(stateBbcode);

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

        var shadowDefault = new TextRuntime();
        shadowDefault.Text = "Soft shadow";
        shadowDefault.FontSize = 24;
        shadowDefault.HasDropshadow = true;
        container.Children.Add(shadowDefault);

        var shadowColored = new TextRuntime();
        shadowColored.Text = "Pink shadow, offset, and blurred";
        shadowColored.FontSize = 24;
        shadowColored.HasDropshadow = true;
        shadowColored.DropshadowColor = new Color(220, 40, 160, 220);
        shadowColored.DropshadowOffsetX = 2;
        shadowColored.DropshadowOffsetY = 4;
        shadowColored.DropshadowBlur = 4;
        container.Children.Add(shadowColored);

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

        // #3670/#3703: CustomFontFile pointing at a bundled .ttf -- previously silently fell back
        // to the default font on both backends. Path differs by backend: MonoGame's
        // FileManager.RelativeDirectory is "Content/" (bare filenames resolve under it, see
        // BearTexture.png elsewhere in this file), while raylib's stays at the exe root and every
        // asset path here includes the "resources/" prefix explicitly (see ShaderFileName in
        // RenderTargetShaderScreen.cs / the 04B_30_.TTF load in Program.cs) -- so the literal string
        // must be gated, not shared, unlike every other path in this file.
        var customFontFileText = new TextRuntime();
        customFontFileText.Text = "I use a bundled .ttf via UseCustomFont + CustomFontFile";
        customFontFileText.FontSize = 24;
        customFontFileText.UseCustomFont = true;
#if RAYLIB
        customFontFileText.CustomFontFile = "resources/Fonts/CustomFont.ttf";
#else
        customFontFileText.CustomFontFile = "Fonts/CustomFont.ttf";
#endif
        container.Children.Add(customFontFileText);

        BuildTextParitySection(container);

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
        _typewriterText.MaxLettersToShow = Math.Min(lettersToShow, TypewriterParagraph.Length);
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
        _typewriterText.MaxLettersToShow = 0;
        container.Children.Add(_typewriterText);
    }

    // Text parity features (#3432): Blend, per-instance TextRenderingPositionMode override, and
    // GetCharacterIndexAtPosition. All three are runtime-observable, so this section is the manual
    // verification surface for the parity batch.
    private static void BuildTextParitySection(ContainerRuntime container)
    {
        AddBlendOnTextSection(container);
        // Placed right after the blend block to match the SilkNetGumSample's section ORDER — the text
        // samples must stay identical in order, not just content.
        AddOverflowSection(container);
        AddTextureFilterSection(container);

        // --- Per-instance TextRenderingPositionMode override, at a fractional origin ---
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
    // share the same FontSize/FontScale. Each cell's own text ("Point" / "Linear") names its filter.
    private static void AddTextureFilterSection(ContainerRuntime container)
    {
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
        pointText.Text = "Point filter (blocky)";
        filterRow.AddChild(pointText);

        RenderingLibrary.Content.ContentLoader.DefaultTextureFilter = Raylib_cs.TextureFilter.Bilinear;
        var linearText = new TextRuntime();
        linearText.FontSize = 13; // distinct size so this is a separate font-cache entry from "Point" above
        linearText.FontScale = 4;
        linearText.Text = "Linear filter (smoothed)";
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
        pointText.Text = "Point filter (blocky)";
        filterRow.AddChild(pointText);
        pointText.MoveToLayer(pointLayer);

        var linearLayer = SystemManagers.Default.Renderer.AddLayer();
        linearLayer.Name = "Texture Filter - Linear";
        linearLayer.IsLinearFilteringEnabled = true;
        var linearText = new TextRuntime();
        linearText.FontSize = 12;
        linearText.FontScale = 4;
        linearText.Text = "Linear filter (smoothed)";
        filterRow.AddChild(linearText);
        linearText.MoveToLayer(linearLayer);
#endif
    }

    // MaxLettersToShow typewriter reveal (#3678). The same wrapping paragraph is shown fully, then
    // with MaxLettersToShow set to a partial count so only the first N letters are visible while the
    // hidden tail still occupies its final layout (reveal is paint-only: WrappedText / measurement
    // stay built from the full RawText). Font = "Arial" because text can silently no-op without a
    // font. Kept content-identical to the SilkNetGumSample text screen.
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
        var row = new TextRuntime();
        row.Font = "Arial";
        row.FontSize = 20;
        row.WidthUnits = DimensionUnitType.Absolute;
        row.Width = 300;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Height = 0;
        row.Text = paragraph;
        row.MaxLettersToShow = maxLetters;
        container.Children.Add(row);
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

    // Overflow modes (#3677): horizontal ellipsis, then vertical TruncateLine vs SpillOver for the
    // same-size box. Each box's text names its own mode. The overflow properties live on TextRuntime
    // (MaxNumberOfLines / IsTruncatingWithEllipsisOnLastLine / TextOverflowVerticalMode), so no cast to
    // the renderable is needed. Kept content-identical to the SilkNetGumSample text screen.
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

    // A fixed-size, dark box so the text's overflow bounds are visible.
    private static RectangleRuntime MakeOverflowBox(float width, float height)
    {
        var box = new RectangleRuntime();
        box.WidthUnits = DimensionUnitType.Absolute;
        box.HeightUnits = DimensionUnitType.Absolute;
        box.Width = width;
        box.Height = height;
        box.FillColor = new Color(40, 40, 40, 255);
        box.IsFilled = true;
        return box;
    }

    // A Text that fills its parent box, so its overflow is exactly the box's bounds.
    private static TextRuntime MakeBoxFillingText(string text)
    {
        var textRuntime = new TextRuntime();
        textRuntime.Text = text;
        textRuntime.Font = "Arial";
        textRuntime.FontSize = 18;
        textRuntime.WidthUnits = DimensionUnitType.RelativeToParent;
        textRuntime.HeightUnits = DimensionUnitType.RelativeToParent;
        textRuntime.Width = 0;
        textRuntime.Height = 0;
        return textRuntime;
    }

}
