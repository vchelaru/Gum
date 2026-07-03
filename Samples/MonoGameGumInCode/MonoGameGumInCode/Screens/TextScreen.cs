using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace MonoGameGumInCode.Screens;

// Reference screen for TextRuntime font behavior. Raylib and SilkNetGum mirror the KernSmith
// baked-shadow rows where that backend supports them (#3414 / #2724).
internal class TextScreen : FrameworkElement
{
    public TextScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var container = new ContainerRuntime();
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.X = 2;
        container.Y = 2;
        container.Width = -4;
        container.Height = -4;
        container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
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

        AddCustomOutlineText(container, Color.Red);
        AddCustomOutlineText(container, Color.DarkGreen);
        AddCustomOutlineText(container, Color.Blue);
    }

    private static void AddSectionLabel(ContainerRuntime container, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        label.FontSize = 14;
        container.Children.Add(label);
    }

    private static void AddCustomOutlineText(ContainerRuntime container, Color color)
    {
        var renderTargetContainer = new ContainerRuntime();
        renderTargetContainer.IsRenderTarget = true;
        renderTargetContainer.Dock(Gum.Wireframe.Dock.SizeToChildren);
        container.AddChild(renderTargetContainer);

        var blendText = new TextRuntime();
        blendText.UseCustomFont = true;
        blendText.FontScale = 1;
        blendText.CustomFontFile =
            "OutlinedFont/Font52Comic_Sans_MS_o4.fnt";
        blendText.Text = "Hello";
        blendText.BlendState = Gum.BlendState.NonPremultiplied.ToXNA();
        renderTargetContainer.Children.Add(blendText);

        var overlay = new ColoredRectangleRuntime();
        overlay.Color = color;
        var blend = Gum.BlendState.MinAlpha.Clone();
        blend.ColorSourceBlend = Gum.Blend.One;
        blend.ColorDestinationBlend = Gum.Blend.Zero;
        blend.ColorBlendFunction = Gum.BlendFunction.Add;
        overlay.BlendState = blend.ToXNA();

        overlay.Dock(Gum.Wireframe.Dock.Fill);
        renderTargetContainer.AddChild(overlay);

        var whiteOverlayText = new TextRuntime();
        whiteOverlayText.UseCustomFont = true;
        whiteOverlayText.FontScale = 1;
        whiteOverlayText.CustomFontFile =
            "OutlinedFont/Font52Comic_Sans_MS_o4.fnt";
        var topBlend = Gum.BlendState.NonPremultiplied.Clone();
        topBlend.ColorSourceBlend = Gum.Blend.One;
        topBlend.ColorDestinationBlend = Gum.Blend.InverseSourceColor;
        topBlend.ColorBlendFunction = Gum.BlendFunction.Add;
        whiteOverlayText.BlendState = topBlend.ToXNA();
        whiteOverlayText.Text = "Hello";
        renderTargetContainer.AddChild(whiteOverlayText);
    }
}
