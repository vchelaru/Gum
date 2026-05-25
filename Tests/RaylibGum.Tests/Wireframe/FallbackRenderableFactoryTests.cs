using Gum.Renderables;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Wireframe;

/// <summary>
/// Unit tests for <see cref="FallbackRenderableFactory.TryHandleAsBaseType"/> as compiled for the
/// Raylib runtime. The same source file backs every runtime via <c>#if</c> fences, but the
/// Raylib path diverges from the XNA-like runtimes: Container/Component always resolve to an
/// <see cref="InvisibleRenderable"/> and never honor <see cref="GraphicalUiElement.ShowLineRectangles"/>.
/// </summary>
public class FallbackRenderableFactoryTests : BaseTestClass
{
    public override void Dispose()
    {
        // The XNA-like FallbackRenderableFactory path reads this static; reset it so the
        // ShowLineRectangles toggle test cannot leak into other tests.
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
        // Unlike the XNA-like runtimes, the Raylib path has no ShowLineRectangles branch:
        // Container must resolve to an InvisibleRenderable regardless of the toggle. This
        // locks in the platform divergence so it is not "fixed" to match MonoGame by accident.
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
        // A non-standard name (e.g. a custom component's own name) is expected to fall through;
        // ElementSaveExtensions.CreateGraphicalComponent relies on this null to recurse into base types.
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("SomeCustomComponent", null);
        result.ShouldBeNull();
    }
}
