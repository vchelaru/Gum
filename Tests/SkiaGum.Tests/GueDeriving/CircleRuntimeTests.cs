using Gum.Wireframe;
using Shouldly;
using SkiaGum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.GueDeriving;

// These tests pin down the post-unification defaults of CircleRuntime on the Skia backend
// (issue #2785). After #2785 lands, the canonical CircleRuntime source lives in
// MonoGameGum/GueDeriving/CircleRuntime.cs and is file-linked into SkiaGum.csproj; Skia's
// previously-divergent 100x100 default is realigned to 32x32 to match MonoGame/Raylib.
// Stroke/fill/dropshadow defaults are still preserved under #if SKIA so existing Skia
// rendering behavior is unchanged for users who instantiate and configure beyond the size.
public class CircleRuntimeTests
{
    public CircleRuntimeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void ContainedRenderable_ShouldBeCircle()
    {
        CircleRuntime sut = new();
        sut.RenderableComponent.ShouldBeOfType<Circle>();
    }

    [Fact]
    public void Height_ShouldBe32_ByDefault()
    {
        CircleRuntime sut = new();
        sut.Height.ShouldBe(32);
    }

    [Fact]
    public void FillColor_ShouldBeNull_ByDefault()
    {
        CircleRuntime sut = new();
        sut.FillColor.ShouldBeNull();
    }

    [Fact]
    public void StrokeColor_ShouldBeWhite_ByDefault()
    {
        CircleRuntime sut = new();
        sut.StrokeColor.ShouldBe(SKColors.White);
    }

    [Fact]
    public void Radius_ShouldBe16_ByDefault()
    {
        CircleRuntime sut = new();
        sut.Radius.ShouldBe(16);
    }

    [Fact]
    public void StrokeWidth_ShouldBe1_ByDefault()
    {
        CircleRuntime sut = new();
        sut.StrokeWidth.ShouldBe(1);
    }

    [Fact]
    public void Width_ShouldBe32_ByDefault()
    {
        CircleRuntime sut = new();
        sut.Width.ShouldBe(32);
    }
}
