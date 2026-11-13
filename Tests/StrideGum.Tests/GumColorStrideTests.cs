using Gum;
using Gum.GueDeriving;
using Shouldly;
using SkiaSharp;
using Stride.Core.Mathematics;
using Color = Gum.Color;
using StrideColor = Stride.Core.Mathematics.Color;

namespace StrideGum.Tests;

/// <summary>
/// Gum.Color converts to and from Stride's Color. A game file that adds <c>using Color = Gum.Color;</c>
/// compiles MonoGame docs snippets unchanged, and the same file still passes the color to Stride calls.
/// The usings above are the ones a game file has: Gum, Stride.Core.Mathematics and the alias.
/// </summary>
public class GumColorStrideTests
{
    [Fact]
    public void ToStrideColor_CopiesEveryChannel()
    {
        StrideColor stride = new Color(10, 20, 30, 40);

        stride.ShouldBe(new StrideColor(10, 20, 30, 40));
    }

    [Fact]
    public void FromStrideColor_CopiesEveryChannel()
    {
        Color gum = new StrideColor(10, 20, 30, 40);

        gum.ShouldBe(new Color(10, 20, 30, 40));
    }

    [Fact]
    public void NamedColor_PassesToStrideCall()
    {
        // A Stride call takes Stride.Core.Mathematics.Color, so the Gum.Color alias must convert.
        StrideColor passed = Pass(Color.CornflowerBlue);

        passed.ShouldBe(new StrideColor(100, 149, 237, 255));
    }

    [Fact]
    public void VisualColor_AcceptsGumColorWithAlias()
    {
        RectangleRuntime rectangle = new();
        rectangle.FillColor = Color.Red;

        rectangle.FillColor.ShouldBe(SKColors.Red);
    }

    private static StrideColor Pass(StrideColor color) => color;
}
