using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace MonoGameGumInCode.Screens;
internal class TextScreen : FrameworkElement
{
    public TextScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var container = new ContainerRuntime();
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        // Give it 2 pixels on each side so text doesn't bump up against the edge of the screen
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

        var withOutline = new TextRuntime();
        withOutline.Text = "I am text that has an outline.";
        (withOutline.Component as RenderingLibrary.Graphics.Text).RenderBoundary = true;
        container.Children.Add(withOutline);

        AddCustomOutlineText(container, Color.Red);
        AddCustomOutlineText(container, Color.DarkGreen);
        AddCustomOutlineText(container, Color.Blue);
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
