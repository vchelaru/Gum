using Gum;
using Gum.GueDeriving;
using Shouldly;
using Stride.Core.Mathematics;

namespace StrideGum.Tests;

/// <summary>
/// Adding Gum.Color to the host must not change what a game file's <c>Color</c> means. This file has the
/// usings every Stride game file has (Gum and Stride.Core.Mathematics) and no alias: <c>Color</c> stays Stride's.
/// </summary>
public class ColorImportsTests
{
    [Fact]
    public void Color_WithGumAndStrideUsings_IsStrideColor()
    {
        Color color = Color.Red;

        color.ShouldBe(Stride.Core.Mathematics.Color.Red);
    }
}
