using Gum.GueDeriving;
using RaylibGum.Helpers;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

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
        var drawingColor = System.Drawing.Color.FromArgb(40, 10, 20, 30);

        sut.SetProperty(nameof(NineSliceRuntime.Color), drawingColor);

        ((Gum.Renderables.NineSlice)sut.RenderableComponent).Color.ShouldBe(drawingColor.ToRaylib());
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
