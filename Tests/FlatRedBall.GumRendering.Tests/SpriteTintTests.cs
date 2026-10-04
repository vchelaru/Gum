using FlatRedBall.Content.ContentLoaders;
using FlatRedBall.GumRendering.Tests.Harness;
using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using DrawingColor = System.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace FlatRedBall.GumRendering.Tests;

/// <summary>
/// The Red, Green, Blue and Alpha variables on a Sprite, set the way a loaded Gum project sets them
/// (<c>SetProperty</c>). Under FRB the shared dispatch has no <c>SpriteRuntime</c> to write to, so these
/// go straight to the <c>Sprite</c>; a regression there is silent.
/// A white opaque texture makes the expected pixel the tint itself; the backdrop is (70,70,90).
/// </summary>
public class SpriteTintTests
{
    private const int Tolerance = 4;
    private static readonly XnaColor Backdrop = new(70, 70, 90);

    [Fact]
    public void RedGreenBlue_TintTheTexture()
    {
        GraphicalUiElement sprite = WhiteSprite();
        sprite.SetProperty("Red", 128);
        sprite.SetProperty("Green", 64);
        sprite.SetProperty("Blue", 255);

        Assert(new XnaColor(128, 64, 255), Draw(sprite));
    }

    [Fact]
    public void Alpha_BlendsOverTheBackdrop()
    {
        GraphicalUiElement sprite = WhiteSprite();
        sprite.SetProperty("Alpha", 128);

        Assert(Over(new XnaColor(255, 255, 255), Backdrop, 128), Draw(sprite));
    }

    [Fact]
    public void TintAndAlpha_Combine()
    {
        GraphicalUiElement sprite = WhiteSprite();
        sprite.SetProperty("Red", 255);
        sprite.SetProperty("Green", 0);
        sprite.SetProperty("Blue", 0);
        sprite.SetProperty("Alpha", 128);

        Assert(Over(new XnaColor(255, 0, 0), Backdrop, 128), Draw(sprite));
    }

    private static XnaColor Over(XnaColor src, XnaColor dst, int alpha)
    {
        float a = alpha / 255f;
        return new XnaColor(
            (int)System.Math.Round(src.R * a + dst.R * (1 - a)),
            (int)System.Math.Round(src.G * a + dst.G * (1 - a)),
            (int)System.Math.Round(src.B * a + dst.B * (1 - a)));
    }

    private static void Assert(XnaColor expected, XnaColor actual)
    {
        string message = $"expected ({expected.R},{expected.G},{expected.B}), was ({actual.R},{actual.G},{actual.B})";
        System.Math.Abs(actual.R - expected.R).ShouldBeLessThanOrEqualTo(Tolerance, message);
        System.Math.Abs(actual.G - expected.G).ShouldBeLessThanOrEqualTo(Tolerance, message);
        System.Math.Abs(actual.B - expected.B).ShouldBeLessThanOrEqualTo(Tolerance, message);
    }

    private static XnaColor Draw(GraphicalUiElement sprite)
    {
        GraphicalUiElement root = Sized(new GraphicalUiElement(new InvisibleRenderable(), null));
        root.Children.Add(Sized(new GraphicalUiElement(new SolidRectangle { Color = DrawingColor.FromArgb(255, Backdrop.R, Backdrop.G, Backdrop.B) }, null)));
        root.Children.Add(sprite);
        return FrbGumHost.Instance.Render(root)[50 * FrbGumHost.Width + 50];
    }

    // An opaque white texture, premultiplied like a real FRB load (a no-op for opaque pixels).
    private static GraphicalUiElement WhiteSprite()
    {
        Texture2D texture = new(FrbGumHost.Instance.GraphicsDevice, 2, 2);
        XnaColor white = new(255, 255, 255, 255);
        texture.SetData(new[] { white, white, white, white });
        texture = TextureContentLoader.MakePremultiplied(texture);
        return Sized(new GraphicalUiElement(new RenderingLibrary.Graphics.Sprite(texture), null));
    }

    private static GraphicalUiElement Sized(GraphicalUiElement element)
    {
        element.Width = 100;
        element.Height = 100;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.HeightUnits = DimensionUnitType.Absolute;
        return element;
    }
}
