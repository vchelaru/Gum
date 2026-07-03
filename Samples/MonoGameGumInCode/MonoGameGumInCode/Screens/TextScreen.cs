using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace MonoGameGumInCode.Screens;

// Reference screen for TextRuntime font behavior. Raylib and SilkNetGum mirror the KernSmith
// baked-shadow rows where that backend supports them (#3414 / #2724).
//
// Section order in this file must match the raylib and SilkNetGum TextScreen.cs mirrors
// (Samples/raylib/Screens/TextScreen.cs, Samples/SilkNetGum/SilkNetGum/Screens/TextScreen.cs)
// exactly, top to bottom - before adding, removing, or reordering ANY section, check the sibling
// files for the same change or the side-by-side comparison breaks silently. (Broke once when this
// file carried extra AddCustomOutlineText rows raylib never had, pushing every later section down
// three rows relative to raylib - #3496.)
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
        withOutline.Text = "I am text that has an outline.";
        (withOutline.Component as Text).RenderBoundary = true;
        container.Children.Add(withOutline);

        AddBlendOnTextSection(container);
        AddTextureFilterSection(container);
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
    }

    private static void AddSectionLabel(ContainerRuntime container, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        label.FontSize = 14;
        container.Children.Add(label);
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
}
