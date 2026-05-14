using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class CircleRuntimeTests : BaseTestClass
{
    // Spike (#2758): when the optional MonoGameGumShapes package is NOT referenced (as is the
    // case for this test project), the ShapeRenderableRegistry factory is never populated.
    // Setting FillColor must degrade gracefully — no crash, and the runtime stays on its
    // outline LineCircle renderable.
    [Fact]
    public void FillColor_WhenSetWithoutShapesPackage_DoesNotThrowAndStaysOutline()
    {
        CircleRuntime sut = new();

        Should.NotThrow(() => sut.FillColor = Color.Red);

        sut.RenderableComponent.ShouldBeOfType<LineCircle>();
    }
}
