using Gum;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Shouldly;

namespace KniSkiaGum.Tests;

/// <summary>
/// Adding Gum.Color to the host must not change what a game file's <c>Color</c> means. This file has the
/// usings every KNI game file has (Gum and Microsoft.Xna.Framework) and no alias: <c>Color</c> stays KNI's.
/// </summary>
public class ColorImportsTests
{
    [Fact]
    public void Color_WithGumAndXnaUsings_IsKniColor()
    {
        Color color = Color.CornflowerBlue;

        color.ShouldBe(Microsoft.Xna.Framework.Color.CornflowerBlue);
    }
}
