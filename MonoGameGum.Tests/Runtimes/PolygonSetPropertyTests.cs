using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Covers the string-property-dispatch path (SetProperty -> CustomSetPropertyOnRenderable ->
// TrySetPropertyOnLinePolygon) for Polygon. Unlike Sprite/NineSlice/Container this dispatch is
// #if !RAYLIB only (raylib's LinePolygon dispatch, if any, is a separate concern), so there is no
// Tests/RaylibGum.Tests counterpart. These properties are set at runtime by the state/variable
// system (StateSave/VariableSave applied via GraphicalUiElement.SetProperty), not by direct C#
// property assignment, so this pins that the string-path dispatch produces the same result as
// direct C# usage (e.g. PolygonRuntime.Alpha = 128).
public class PolygonSetPropertyTests : BaseTestClass
{
    [Fact]
    public void SetProperty_AlphaRedGreenBlue_ShouldForwardToPolygonRuntime()
    {
        PolygonRuntime sut = new();

        sut.SetProperty(nameof(PolygonRuntime.Alpha), 128);
        sut.SetProperty(nameof(PolygonRuntime.Red), 10);
        sut.SetProperty(nameof(PolygonRuntime.Green), 20);
        sut.SetProperty(nameof(PolygonRuntime.Blue), 30);

        sut.Alpha.ShouldBe(128);
        sut.Red.ShouldBe(10);
        sut.Green.ShouldBe(20);
        sut.Blue.ShouldBe(30);
    }

    [Fact]
    public void SetProperty_Color_ShouldForwardToPolygonRuntime()
    {
        PolygonRuntime sut = new();
        var drawingColor = System.Drawing.Color.FromArgb(255, 10, 20, 30);

        sut.SetProperty(nameof(PolygonRuntime.Color), drawingColor);

        sut.Color.R.ShouldBe((byte)10);
        sut.Color.G.ShouldBe((byte)20);
        sut.Color.B.ShouldBe((byte)30);
    }
}
