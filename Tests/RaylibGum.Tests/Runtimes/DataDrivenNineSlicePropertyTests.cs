using Gum.GueDeriving;
using Gum.Renderables;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// Reproduces the data-driven property-application path that loading a .gumx project uses
/// (<c>ApplyState</c> → <c>SetProperty(string, object)</c>), for NineSlice properties dispatched
/// through <c>CustomSetPropertyOnRenderable.TrySetPropertyOnNineSlice</c>. These pin that the
/// explicit dispatch cases produce the same result as the generic reflection fallback they
/// replace (issue #3615 NineSlice convergence).
/// </summary>
public class DataDrivenNineSlicePropertyTests : BaseTestClass
{
    [Fact]
    public void SetProperty_CustomFrameTextureCoordinateWidth_OnNineSlice_AppliesValue()
    {
        NineSliceRuntime sut = new();

        Should.NotThrow(() => sut.SetProperty("CustomFrameTextureCoordinateWidth", 7f));

        ((NineSlice)sut.RenderableComponent).CustomFrameTextureCoordinateWidth.ShouldBe(7f);
    }

    [Fact]
    public void SetProperty_BorderScale_OnNineSlice_AppliesValue()
    {
        NineSliceRuntime sut = new();

        Should.NotThrow(() => sut.SetProperty("BorderScale", 3f));

        ((NineSlice)sut.RenderableComponent).BorderScale.ShouldBe(3f);
    }

    [Fact]
    public void SetProperty_IsTilingMiddleSections_OnNineSlice_AppliesValue()
    {
        NineSliceRuntime sut = new();

        Should.NotThrow(() => sut.SetProperty("IsTilingMiddleSections", true));

        ((NineSlice)sut.RenderableComponent).IsTilingMiddleSections.ShouldBeTrue();
    }

    // Color -> Raylib_cs.Color has no converter yet (#3629), so this data-driven path is a
    // tracked no-op. This pins that it stays a silent no-op rather than throwing.
    [Fact]
    public void SetProperty_Color_OnNineSlice_DoesNotThrow()
    {
        NineSliceRuntime sut = new();

        Should.NotThrow(() => sut.SetProperty("Color", System.Drawing.Color.Red));
    }
}
