using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Add is a <see cref="ColorOperation"/> like Modulate, driven by the renderable's own Color
/// rather than a second color field (#4880).
/// </summary>
public class ColorOperationAddTests
{
    [Fact]
    public void ColorOperation_ShouldForwardToAllContainedSprites_OnNineSliceRuntime()
    {
        NineSliceRuntime nineSlice = new();

        nineSlice.ColorOperation = ColorOperation.Add;

        nineSlice.ColorOperation.ShouldBe(ColorOperation.Add);
        ((NineSlice)nineSlice.RenderableComponent).ColorOperation.ShouldBe(ColorOperation.Add);
    }
}
