using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;

namespace Examples.Shapes;

// Raylib mirror of Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/TextScreen.cs (#3414).
// Section order matches the MG screen so baked-shadow regressions are easy to spot side by side.
// Requires KernSmithRaylibFontCreator wired in Program.Main.
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

        AddSectionLabel(container, "BBCode markup - inline color runs (#3471):");
        var colorMarkup = new TextRuntime();
        colorMarkup.FontSize = 24;
        colorMarkup.Text = "[Color=Red]Red[/Color] plain [Color=Lime]green[/Color] [Color=Cyan]cyan[/Color]";
        container.Children.Add(colorMarkup);

        AddSectionLabel(container, "BBCode markup - inline FontScale runs (baseline aligned):");
        var scaleMarkup = new TextRuntime();
        scaleMarkup.FontSize = 24;
        scaleMarkup.Text = "small [FontScale=2]BIG[/FontScale] then [Color=Orange][FontScale=1.5]orange[/FontScale][/Color]";
        container.Children.Add(scaleMarkup);

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

    // #3432 raylib Text parity: Blend, per-instance TextRenderingPositionMode override, and
    // GetCharacterIndexAtPosition. All three are runtime-observable, so this section is the manual
    // verification surface for the parity batch.
    private static void BuildTextParitySection(ContainerRuntime container)
    {
        AddBlendOnTextSection(container);

        // --- Per-instance TextRenderingPositionMode override, at a fractional origin ---
        AddSectionLabel(container, "TextRenderingPositionMode (#3432): fractional origin; button toggles snap");
        var snapText = new TextRuntime();
        snapText.FontSize = 20;
        snapText.X = 120.5f;
        snapText.Text = "Fractional X=120.5 - SnapToPixel";
        snapText.TextRenderingPositionMode = Gum.Renderables.TextRenderingPositionMode.SnapToPixel;
        container.Children.Add(snapText);

        var toggleButton = new Button();
        toggleButton.Text = "Toggle snap mode";
        toggleButton.Click += (_, _) =>
        {
            if (snapText.TextRenderingPositionMode == Gum.Renderables.TextRenderingPositionMode.SnapToPixel)
            {
                snapText.TextRenderingPositionMode = Gum.Renderables.TextRenderingPositionMode.FreeFloating;
                snapText.Text = "Fractional X=120.5 - FreeFloating";
            }
            else
            {
                snapText.TextRenderingPositionMode = Gum.Renderables.TextRenderingPositionMode.SnapToPixel;
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

    // Blend on Text (#3432): additive (brightens) vs normal, over an identical blue box. Kept byte
    // identical with the other backend's TextScreen (this method and BuildBlendCell) so the two can
    // be diffed directly.
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

    private static void AddSectionLabel(ContainerRuntime container, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        label.FontSize = 14;
        container.Children.Add(label);
    }
}
