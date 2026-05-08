using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

public class CircleRuntimeTests : BaseTestClass
{
    [Fact]
    public void Radius_ShouldBe16_ByDefault()
    {
        CircleRuntime cut = new();
        cut.Radius.ShouldBe(16);
    }
}