using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Covers the string-property-dispatch path (SetProperty -> CustomSetPropertyOnRenderable ->
// TrySetPropertyOnNineSlice) for NineSlice, mirroring SpriteSetPropertyTests.cs. These properties
// are set at runtime by the state/variable system (StateSave/VariableSave applied via
// GraphicalUiElement.SetProperty), not by direct C# property assignment, so this pins that the
// string-path dispatch produces the same result as direct C# usage (e.g. NineSliceRuntime.Red = 10).
public class NineSliceSetPropertyTests : BaseTestClass
{
    [Fact]
    public void SetProperty_BorderScale_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty(nameof(NineSliceRuntime.BorderScale), 2f);

        sut.BorderScale.ShouldBe(2f);
    }

    [Fact]
    public void SetProperty_Blend_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty("Blend", Gum.RenderingLibrary.Blend.Additive);

        sut.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Additive);
    }

    [Fact]
    public void SetProperty_Color_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new();
        var drawingColor = System.Drawing.Color.FromArgb(255, 10, 20, 30);

        sut.SetProperty(nameof(NineSliceRuntime.Color), drawingColor);

        sut.Color.R.ShouldBe((byte)10);
        sut.Color.G.ShouldBe((byte)20);
        sut.Color.B.ShouldBe((byte)30);
    }

    [Fact]
    public void SetProperty_Color_WithXnaColorValue_ShouldForwardToContainedNineSlice()
    {
        // The dispatcher accepts both System.Drawing.Color and Microsoft.Xna.Framework.Color as
        // the incoming value (state/variable values are stored as System.Drawing.Color, but direct
        // callers can pass XNA's Color) - this pins the XNA-typed branch, which needs no conversion
        // since NineSliceRuntime.Color is itself XNA-typed on this backend.
        NineSliceRuntime sut = new();
        var xnaColor = new Microsoft.Xna.Framework.Color(10, 20, 30, 255);

        sut.SetProperty(nameof(NineSliceRuntime.Color), xnaColor);

        sut.Color.R.ShouldBe((byte)10);
        sut.Color.G.ShouldBe((byte)20);
        sut.Color.B.ShouldBe((byte)30);
    }

    [Fact]
    public void SetProperty_CustomFrameTextureCoordinateWidth_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty(nameof(NineSliceRuntime.CustomFrameTextureCoordinateWidth), 4f);

        sut.CustomFrameTextureCoordinateWidth.ShouldBe(4f);
    }

    [Fact]
    public void SetProperty_IsTilingMiddleSections_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty(nameof(NineSliceRuntime.IsTilingMiddleSections), true);

        sut.IsTilingMiddleSections.ShouldBeTrue();
    }

    [Fact]
    public void SetProperty_RedGreenBlue_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty(nameof(NineSliceRuntime.Red), 10);
        sut.SetProperty(nameof(NineSliceRuntime.Green), 20);
        sut.SetProperty(nameof(NineSliceRuntime.Blue), 30);

        sut.Red.ShouldBe(10);
        sut.Green.ShouldBe(20);
        sut.Blue.ShouldBe(30);
    }
}
