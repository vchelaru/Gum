using FlatRedBall.Content.ContentLoaders;
using FlatRedBall.GumRendering.Tests.Harness;
using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using Blend = Gum.RenderingLibrary.Blend;
using DrawingColor = System.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace FlatRedBall.GumRendering.Tests;

/// <summary>
/// Single-pixel blend math through FRB's premultiplied pipeline, with solid colors and a known alpha so
/// the expected value is arithmetic instead of a golden. The source is red at alpha 128 over a
/// (70,70,90) backdrop. FRB premultiplies textures at load, so sprite sources go through
/// <c>MakePremultiplied</c> like real ones.
/// </summary>
public class BlendMathTests
{
    private const int Alpha = 128;
    private const string RenderTargetModesBroken =
        "Known FRB bug: alpha-mask blends in a render target come out darker/wrong (#5690). Un-skip when fixed.";

    private static readonly DrawingColor Backdrop = DrawingColor.FromArgb(255, 70, 70, 90);
    private static readonly DrawingColor Fill = DrawingColor.FromArgb(255, 200, 30, 30);
    private static readonly XnaColor BackdropColor = new(70, 70, 90);

    // Straight-alpha math: out = src * a + dst * (1 - a), with a = 128 / 255.
    private static XnaColor Over(XnaColor src, XnaColor dst)
    {
        float a = Alpha / 255f;
        return new XnaColor(
            (int)System.Math.Round(src.R * a + dst.R * (1 - a)),
            (int)System.Math.Round(src.G * a + dst.G * (1 - a)),
            (int)System.Math.Round(src.B * a + dst.B * (1 - a)));
    }

    // Red at alpha 128 added to the backdrop: backdrop + red * a, clamped.
    private static XnaColor AddRed() =>
        new(System.Math.Min(255, BackdropColor.R + (int)System.Math.Round(255 * Alpha / 255f)), BackdropColor.G, BackdropColor.B);

    // A half-alpha mask leaves the opaque fill at alpha 128 for all three alpha-mask modes, over the backdrop.
    private static XnaColor MaskedFill() => Over(new XnaColor(Fill.R, Fill.G, Fill.B), BackdropColor);

    // Half-alpha content baked in a render target and composited back must keep its color: baked texel is
    // premultiplied, so the blit must not scale it by alpha again.
    [Fact]
    public void RenderTarget_HalfAlphaContent_KeepsColor()
    {
        GraphicalUiElement root = Container(renderTarget: false);
        root.Children.Add(Solid(Backdrop));
        GraphicalUiElement target = Container(renderTarget: true);
        target.Children.Add(Solid(DrawingColor.FromArgb(Alpha, Fill.R, Fill.G, Fill.B)));
        root.Children.Add(target);
        Assert(MaskedFill(), Center(FrbGumHost.Instance.Render(root)));
    }

    // Container Alpha 128 over opaque content. The tint is premultiplied in DrawRenderTargetToScreen and
    // again in Sprite.Render, so the color comes out at 25% instead of 50%.
    [Fact(Skip = "Known FRB bug: render-target container Alpha is premultiplied twice (#5690). Un-skip when fixed.")]
    public void RenderTarget_ContainerAlpha_ScalesColorOnce()
    {
        GraphicalUiElement root = Container(renderTarget: false);
        root.Children.Add(Solid(Backdrop));
        GraphicalUiElement target = Container(renderTarget: true);
        target.Children.Add(Solid(Fill));
        target.SetProperty("Alpha", Alpha);
        root.Children.Add(target);
        Assert(MaskedFill(), Center(FrbGumHost.Instance.Render(root)));
    }

    [Fact]
    public void Normal_Rectangle() =>
        Assert(Over(new XnaColor(255, 0, 0), BackdropColor), Draw(Solid(Red()), Blend.Normal));

    [Fact]
    public void Normal_Sprite() =>
        Assert(Over(new XnaColor(255, 0, 0), BackdropColor), Draw(Sprite(), Blend.Normal));

    // Additive was SourceAlpha,One, which scaled an already-premultiplied source by alpha twice (+64 instead of +128).
    [Fact]
    public void Additive_Rectangle() => Assert(AddRed(), Draw(Solid(Red()), Blend.Additive));

    [Fact]
    public void Additive_Sprite() => Assert(AddRed(), Draw(Sprite(), Blend.Additive));

    [Fact]
    public void SubtractAlpha_Rectangle() => Assert(MaskedFill(), Mask(Solid(WhiteHalf()), Blend.SubtractAlpha));

    [Fact]
    public void SubtractAlpha_Sprite() => Assert(MaskedFill(), Mask(Sprite(white: true), Blend.SubtractAlpha));

    [Fact]
    public void ReplaceAlpha_Rectangle() => Assert(MaskedFill(), Mask(Solid(WhiteHalf()), Blend.ReplaceAlpha));

    [Fact]
    public void ReplaceAlpha_Sprite() => Assert(MaskedFill(), Mask(Sprite(white: true), Blend.ReplaceAlpha));

    [Fact]
    public void MinAlpha_Rectangle() => Assert(MaskedFill(), Mask(Solid(WhiteHalf()), Blend.MinAlpha));

    [Fact]
    public void MinAlpha_Sprite() => Assert(MaskedFill(), Mask(Sprite(white: true), Blend.MinAlpha));

    private const int Tolerance = 4;

    private static void Assert(XnaColor expected, XnaColor actual)
    {
        string message = $"expected ({expected.R},{expected.G},{expected.B}), was ({actual.R},{actual.G},{actual.B})";
        System.Math.Abs(actual.R - expected.R).ShouldBeLessThanOrEqualTo(Tolerance, message);
        System.Math.Abs(actual.G - expected.G).ShouldBeLessThanOrEqualTo(Tolerance, message);
        System.Math.Abs(actual.B - expected.B).ShouldBeLessThanOrEqualTo(Tolerance, message);
    }

    // Draws backdrop + source with the given blend and reads the pixel at the source's center.
    private static XnaColor Draw(GraphicalUiElement source, Blend blend)
    {
        source.SetProperty("Blend", blend);
        GraphicalUiElement root = Container(renderTarget: false);
        root.Children.Add(Solid(Backdrop));
        root.Children.Add(source);
        return Center(FrbGumHost.Instance.Render(root));
    }

    // Draws backdrop + a render-target container (opaque fill + mask with the given blend).
    private static XnaColor Mask(GraphicalUiElement mask, Blend blend)
    {
        mask.SetProperty("Blend", blend);
        GraphicalUiElement root = Container(renderTarget: false);
        root.Children.Add(Solid(Backdrop));
        GraphicalUiElement target = Container(renderTarget: true);
        target.Children.Add(Solid(Fill));
        target.Children.Add(mask);
        root.Children.Add(target);
        return Center(FrbGumHost.Instance.Render(root));
    }

    private static XnaColor Center(XnaColor[] pixels) => pixels[50 * FrbGumHost.Width + 50];

    private static DrawingColor Red() => DrawingColor.FromArgb(Alpha, 255, 0, 0);

    private static DrawingColor WhiteHalf() => DrawingColor.FromArgb(Alpha, 255, 255, 255);

    private static GraphicalUiElement Container(bool renderTarget)
    {
        GraphicalUiElement container = new(new InvisibleRenderable { IsRenderTarget = renderTarget }, null)
        {
            Width = 100, Height = 100,
        };
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;
        return container;
    }

    // 100x100 keeps (50,50) in the middle of every source.
    private static GraphicalUiElement Solid(DrawingColor color)
    {
        GraphicalUiElement element = new(new SolidRectangle { Color = color }, null) { Width = 100, Height = 100 };
        element.WidthUnits = DimensionUnitType.Absolute;
        element.HeightUnits = DimensionUnitType.Absolute;
        return element;
    }

    private static GraphicalUiElement Sprite(bool white = false)
    {
        Texture2D texture = new(FrbGumHost.Instance.GraphicsDevice, 2, 2);
        XnaColor straight = white ? new XnaColor(255, 255, 255, Alpha) : new XnaColor(255, 0, 0, Alpha);
        texture.SetData(new[] { straight, straight, straight, straight });
        texture = TextureContentLoader.MakePremultiplied(texture);

        GraphicalUiElement sprite = new(new RenderingLibrary.Graphics.Sprite(texture), null) { Width = 100, Height = 100 };
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        return sprite;
    }
}
