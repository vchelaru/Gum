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

        // Skia-only standalone text effects set directly on the renderable (issue #3674 drop shadow,
        // issue #3675 outline). Unlike the baked-shadow rows above (TextRuntime.HasDropshadow, the
        // MonoGame/KNI/Raylib font-atlas path that no-ops on Skia), these render on SkiaGum. This
        // section has no MonoGameGumInCode mirror — it exercises the SkiaGum.Text renderable directly.
        AddStandaloneSkiaEffectsSection(container);

        AddOverflowSection(container);
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
        shadowed.FontSize = 48;
        shadowed.Red = 255;
        shadowed.Green = 255;
        shadowed.Blue = 255;

        SkiaGum.Text shadowedRenderable = (SkiaGum.Text)shadowed.RenderableComponent;
        shadowedRenderable.HasDropshadow = true;
        shadowedRenderable.DropshadowOffsetX = 3;
        shadowedRenderable.DropshadowOffsetY = 3;
        shadowedRenderable.DropshadowBlurX = 6;
        shadowedRenderable.DropshadowBlurY = 6;
        shadowedRenderable.DropshadowColor = new SKColor(0, 0, 0, 255);
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
