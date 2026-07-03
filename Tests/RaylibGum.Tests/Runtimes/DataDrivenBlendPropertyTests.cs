using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Gum.Renderables;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// Reproduces the data-driven property-application path that loading a .gumx project uses
/// (<c>ApplyState</c> → <c>SetProperty(string, object)</c> → reflection), as opposed to the
/// strongly-typed C# setters the other Blend tests use. The nullable <c>Blend?</c> renderable
/// properties crash on this path because <c>Convert.ChangeType</c> cannot convert an enum to its
/// <c>Nullable&lt;&gt;</c> form (issue surfaced as an InvalidCastException setting Blend on a
/// NineSlice when constructing a Forms screen).
/// </summary>
public class DataDrivenBlendPropertyTests : BaseTestClass
{
    [Fact]
    public void SetProperty_Blend_OnNineSlice_AppliesWithoutThrowing()
    {
        NineSliceRuntime sut = new();

        Should.NotThrow(() => sut.SetProperty("Blend", Blend.Normal));

        ((NineSlice)sut.RenderableComponent).Blend.ShouldBe(Blend.Normal);
    }

    // Issue #3458 — the .gumx "Blend" variable reaches the raylib RectangleRuntime the same way:
    // reflection sets Blend on the contained LineRectangle (now that it exposes a nullable Blend).
    [Fact]
    public void SetProperty_Blend_OnRectangle_AppliesWithoutThrowing()
    {
        RectangleRuntime sut = new();

        Should.NotThrow(() => sut.SetProperty("Blend", Blend.Additive));

        ((LineRectangle)sut.RenderableComponent).Blend.ShouldBe(Blend.Additive);
    }

    [Fact]
    public void SetProperty_Blend_OnSprite_AppliesWithoutThrowing()
    {
        SpriteRuntime sut = new();

        Should.NotThrow(() => sut.SetProperty("Blend", Blend.Additive));

        ((Sprite)sut.RenderableComponent).Blend.ShouldBe(Blend.Additive);
    }

    // The shape Color? properties (FillColor/StrokeColor) are the same nullable-target class as
    // Blend? — a Color value into a Color? property would have hit the identical Convert.ChangeType
    // crash on the data-driven path. These pin that the general Nullable<T> fix covers them too.

    [Fact]
    public void SetProperty_FillColor_OnCircle_AppliesWithoutThrowing()
    {
        CircleRuntime sut = new();
        Raylib_cs.Color expected = new(10, 20, 30, 40);

        Should.NotThrow(() => sut.SetProperty("FillColor", expected));

        ((Gum.Renderables.LineCircle)sut.RenderableComponent).FillColor.ShouldBe(expected);
    }

    [Fact]
    public void SetProperty_StrokeColor_OnRectangle_AppliesWithoutThrowing()
    {
        RectangleRuntime sut = new();
        Raylib_cs.Color expected = new(50, 60, 70, 80);

        Should.NotThrow(() => sut.SetProperty("StrokeColor", expected));

        ((Gum.Renderables.LineRectangle)sut.RenderableComponent).StrokeColor.ShouldBe(expected);
    }
}
