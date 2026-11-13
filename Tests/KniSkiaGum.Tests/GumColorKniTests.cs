using Gum;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Shouldly;
using SkiaSharp;
using Color = Gum.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace KniSkiaGum.Tests;

/// <summary>
/// Gum.Color converts to and from KNI's Color. A game file that adds <c>using Color = Gum.Color;</c>
/// compiles MonoGame docs snippets unchanged, and the same file still passes the color to KNI calls.
/// The usings above are the ones a game file has: Gum, Microsoft.Xna.Framework and the alias.
/// </summary>
public class GumColorKniTests
{
    [Fact]
    public void ToKniColor_CopiesEveryChannel()
    {
        XnaColor kni = new Color(10, 20, 30, 40);

        kni.ShouldBe(new XnaColor(10, 20, 30, 40));
    }

    [Fact]
    public void FromKniColor_CopiesEveryChannel()
    {
        Color gum = new XnaColor(10, 20, 30, 40);

        gum.ShouldBe(new Color(10, 20, 30, 40));
    }

    [Fact]
    public void NamedColor_PassesToKniCall()
    {
        // GraphicsDevice.Clear(Color.CornflowerBlue) is the call every KNI game file makes.
        XnaColor cleared = Pass(Color.CornflowerBlue);

        cleared.ShouldBe(XnaColor.CornflowerBlue);
    }

    [Fact]
    public void VisualColor_AcceptsGumColorWithAlias()
    {
        RectangleRuntime rectangle = new();
        rectangle.FillColor = Color.Red;

        rectangle.FillColor.ShouldBe(SKColors.Red);
    }

    private static XnaColor Pass(XnaColor color) => color;
}
