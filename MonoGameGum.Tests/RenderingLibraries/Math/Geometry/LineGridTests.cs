using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Math.Geometry;

public class LineGridTests : BaseTestClass
{
    [Fact]
    public void X_And_Y_ShouldRoundTrip()
    {
        // Regression: X/Y used to be hardcoded no-op stubs (always 0), which made it impossible to
        // reposition a LineGrid overlay - needed so the grid-snap overlay (issue #4137) can be kept
        // aligned to the canvas grid as the camera pans.
        LineGrid sut = new(SystemManagers.Default);
        IPositionedSizedObject ipso = sut;

        ipso.X = 32;
        ipso.Y = -16;

        ipso.X.ShouldBe(32);
        ipso.Y.ShouldBe(-16);
    }
}
