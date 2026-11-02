using Gum.GueDeriving;
using Gum.Renderables;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// #7302 - the Arc standard's gradient start variables (Red1/Green1/Blue1/Alpha1) must reach the
/// arc's gradient start, which is the body color on every backend.
/// </summary>
public class ArcGradientStartTests : BaseTestClass
{
    [Fact]
    public void SetProperty_Red1Green1Blue1Alpha1_SetsGradientStartOnRenderable()
    {
        ArcRuntime arc = new();

        arc.SetProperty("Red1", 91);
        arc.SetProperty("Green1", 161);
        arc.SetProperty("Blue1", 207);
        arc.SetProperty("Alpha1", 128);
        arc.PreRender();

        LineArc renderable = (LineArc)arc.RenderableComponent!;
        renderable.Color1.R.ShouldBe((byte)91);
        renderable.Color1.G.ShouldBe((byte)161);
        renderable.Color1.B.ShouldBe((byte)207);
        renderable.Color1.A.ShouldBe((byte)128);
    }
}
