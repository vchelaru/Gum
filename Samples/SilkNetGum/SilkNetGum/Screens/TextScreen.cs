using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using SkiaSharp;

namespace SilkNetGum.Screens;

// Mirror of Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/TextScreen.cs where the backend
// supports the same surface (#3414). SkiaGum dynamic fonts do not use KernSmith, so baked text
// drop shadow (HasDropshadow) is not rendered here — the rows below document that gap explicitly.
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

        AddSectionLabel(container,
            "Baked drop shadow requires KernSmith (MonoGame / KNI / Raylib). SkiaGum stores HasDropshadow but does not bake a shadow atlas — rows below show plain text for layout parity only:");

        TextRuntime shadowDefault = new TextRuntime();
        shadowDefault.Text = "Soft baked shadow (not on Skia)";
        shadowDefault.FontSize = 24;
        shadowDefault.HasDropshadow = true;
        container.Children.Add(shadowDefault);

        TextRuntime shadowColored = new TextRuntime();
        shadowColored.Text = "Pink shadow (not on Skia)";
        shadowColored.FontSize = 24;
        shadowColored.HasDropshadow = true;
        shadowColored.DropshadowColor = new SKColor(220, 40, 160, 220);
        shadowColored.DropshadowOffsetX = 2;
        shadowColored.DropshadowOffsetY = 4;
        shadowColored.DropshadowBlur = 4;
        container.Children.Add(shadowColored);

        TextRuntime withOutline = new TextRuntime();
        withOutline.Text = "OutlineThickness = 2 (Skia path)";
        withOutline.FontSize = 24;
        withOutline.OutlineThickness = 2;
        container.Children.Add(withOutline);

        AddTextureFilterSection(container);
    }

    // Texture filter on Text (#3496): mirrored for structural parity with MonoGameGumInCode and
    // raylib, but SkiaGum text has no Point/Linear knob to demonstrate. SkiaGum draws glyphs each
    // frame via Topten.RichTextKit straight onto the canvas (SkiaSharp's own font rasterizer) rather
    // than sampling a pre-baked font atlas texture with a bilinear/point sampler, so there is no
    // texture-filter render state for text on this backend - both cells below render identically.
    private static void AddTextureFilterSection(ContainerRuntime container)
    {
        AddSectionLabel(container,
            "Texture filter (#3496): no Point/Linear distinction on Skia text - see comment above AddTextureFilterSection");
        ContainerRuntime filterRow = new ContainerRuntime();
        filterRow.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        filterRow.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        filterRow.Width = 0;
        filterRow.Height = 0;
        filterRow.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        filterRow.StackSpacing = 16;

        TextRuntime pointText = new TextRuntime();
        pointText.FontSize = 12;
        pointText.FontScale = 4;
        pointText.Text = "Point";
        filterRow.AddChild(pointText);

        TextRuntime linearText = new TextRuntime();
        linearText.FontSize = 12;
        linearText.FontScale = 4;
        linearText.Text = "Linear";
        filterRow.AddChild(linearText);

        container.Children.Add(filterRow);
    }

    private static void AddSectionLabel(ContainerRuntime container, string text)
    {
        TextRuntime label = new TextRuntime();
        label.Text = text;
        label.FontSize = 14;
        container.Children.Add(label);
    }
}
