using Gum.Renderables;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;

namespace SokolGum.Tests.Wireframe;

/// <summary>
/// Unit tests for <see cref="FallbackRenderableFactory.TryHandleAsBaseType"/> as compiled for the
/// Sokol runtime. The same source file backs every runtime via <c>#if</c> fences; the Sokol
/// path shares the Raylib branch where it matters (Container/Component always resolve to an
/// <see cref="InvisibleRenderable"/>) and uses Sokol's own renderable types for everything
/// else.
/// </summary>
public class FallbackRenderableFactoryTests : BaseTestClass
{
    public override void Dispose()
    {
        GraphicalUiElement.ShowLineRectangles = false;
        base.Dispose();
    }

    [Fact]
    public void TryHandleAsBaseType_Circle_ReturnsLineCircle()
    {
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Circle", null);
        result.ShouldBeOfType<LineCircle>();
    }

    [Fact]
    public void TryHandleAsBaseType_ColoredRectangle_ReturnsSolidRectangle()
    {
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("ColoredRectangle", null);
        result.ShouldBeOfType<SolidRectangle>();
    }

    [Fact]
    public void TryHandleAsBaseType_Component_ReturnsInvisibleRenderable()
    {
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Component", null);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void TryHandleAsBaseType_Container_ReturnsInvisibleRenderable()
    {
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Container", null);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void TryHandleAsBaseType_Container_ReturnsInvisibleRenderable_EvenWhenShowLineRectanglesIsTrue()
    {
        // The Sokol path shares Raylib's behavior: Container resolves to InvisibleRenderable
        // regardless of the ShowLineRectangles toggle. This locks in the platform divergence
        // so it is not "fixed" to match the MonoGame branch by accident.
        GraphicalUiElement.ShowLineRectangles = true;

        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Container", null);

        result.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void TryHandleAsBaseType_NineSlice_ReturnsNineSlice()
    {
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("NineSlice", null);
        result.ShouldBeOfType<NineSlice>();
    }

    [Fact]
    public void TryHandleAsBaseType_Polygon_ReturnsLinePolygon()
    {
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Polygon", null);
        result.ShouldBeOfType<LinePolygon>();
    }

    [Fact]
    public void TryHandleAsBaseType_Rectangle_ReturnsLineRectangle()
    {
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Rectangle", null);
        result.ShouldBeOfType<LineRectangle>();
    }

    [Fact]
    public void TryHandleAsBaseType_Sprite_ReturnsSprite()
    {
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Sprite", null);
        result.ShouldBeOfType<Sprite>();
    }

    [Fact]
    public void TryHandleAsBaseType_Text_ReturnsText()
    {
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Text", null);
        result.ShouldBeOfType<Text>();
    }

    [Fact]
    public void TryHandleAsBaseType_UnrecognizedType_ReturnsNull()
    {
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("SomeCustomComponent", null);
        result.ShouldBeNull();
    }
}
