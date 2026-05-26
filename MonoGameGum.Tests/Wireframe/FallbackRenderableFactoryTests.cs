using Gum.GueDeriving;
using Gum.Wireframe;
using MonoGameGum.Renderables;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Wireframe;

/// <summary>
/// Unit tests for <see cref="FallbackRenderableFactory.TryHandleAsBaseType"/>, the factory that
/// maps a Gum standard element base type name to its backing <see cref="IRenderable"/>.
/// This type is compiled into every XNA-like runtime and the Gum tool, so a regression here
/// breaks both runtime rendering and the editor.
/// </summary>
public class FallbackRenderableFactoryTests : BaseTestClass
{
    public override void Dispose()
    {
        // TryHandleAsBaseType reads this static; restore the default so toggle tests don't leak.
        GraphicalUiElement.ShowLineRectangles = false;
        base.Dispose();
    }

    [Fact]
    public void TryHandleAsBaseType_Circle_ReturnsLineCircle_WhenNoRegistryFactory()
    {
        // BaseTestClass.Dispose runs RenderableRegistry.Reset(), so each test starts with no
        // factories. Without a registered IStrokedCircleRenderable factory, the fallback's
        // legacy LineCircle path must still fire so the tool keeps rendering circles when
        // MonoGameGumShapes is not in use.
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Circle", null);
        result.ShouldBeOfType<LineCircle>();
    }

    [Fact]
    public void TryHandleAsBaseType_Circle_PrefersRegistryFactory_OverLegacyLineCircle()
    {
        // Issue #2925: the tool's editor wireframe path (EditorTabPlugin_XNA.HandleCreate
        // RenderableForType) calls TryHandleAsBaseType for "Circle". When MonoGameGumShapes
        // is loaded it registers an Apos.Shapes-backed IStrokedCircleRenderable factory;
        // the fallback must ask the registry first so the user sees the Apos.Shapes
        // shader-drawn circle (smooth at any zoom) rather than the legacy LineCircle's
        // polygon segments.
        RegistryStrokedCircleSentinel sentinel = new();
        RenderableRegistry.RegisterFactory<IStrokedCircleRenderable>(() => sentinel);

        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Circle", null);

        result.ShouldBeSameAs(sentinel);
    }

    [Fact]
    public void TryHandleAsBaseType_ColoredRectangle_ReturnsSolidRectangle()
    {
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("ColoredRectangle", null);
        result.ShouldBeOfType<SolidRectangle>();
    }

    [Fact]
    public void TryHandleAsBaseType_Component_ReturnsNonNull_WhenShowLineRectanglesIsFalse()
    {
        // Regression guard for PR #2746: dropping the else branch made Component return null
        // whenever ShowLineRectangles was false (its default), breaking old/XML-error projects.
        GraphicalUiElement.ShowLineRectangles = false;

        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Component", null);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void TryHandleAsBaseType_Container_ReturnsLineRectangle_WhenShowLineRectanglesIsTrue()
    {
        GraphicalUiElement.ShowLineRectangles = true;

        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Container", null);

        result.ShouldBeOfType<LineRectangle>();
    }

    [Fact]
    public void TryHandleAsBaseType_Container_ReturnsNonNull_WhenShowLineRectanglesIsFalse()
    {
        // Regression guard for PR #2746: with ShowLineRectangles false (the default, and the
        // common case in the Gum tool), Container must still resolve to an InvisibleRenderable
        // rather than null. A null here leaves the GraphicalUiElement with no contained object.
        GraphicalUiElement.ShowLineRectangles = false;

        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Container", null);

        result.ShouldNotBeNull();
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
    public void TryHandleAsBaseType_Rectangle_ReturnsLineRectangle_WhenNoRegistryFactory()
    {
        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Rectangle", null);
        result.ShouldBeOfType<LineRectangle>();
    }

    [Fact]
    public void TryHandleAsBaseType_Rectangle_PrefersRegistryFactory_OverLegacyLineRectangle()
    {
        // Issue #2925 mirror of the Circle test above: the Apos.Shapes-backed
        // IStrokedRectangleRenderable factory must override the legacy LineRectangle in the
        // tool's wireframe path when MonoGameGumShapes is loaded.
        RegistryStrokedRectangleSentinel sentinel = new();
        RenderableRegistry.RegisterFactory<IStrokedRectangleRenderable>(() => sentinel);

        IRenderable result = FallbackRenderableFactory.TryHandleAsBaseType("Rectangle", null);

        result.ShouldBeSameAs(sentinel);
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

    // Sentinel subclass of the core default so ShouldBeSameAs can verify the registry
    // factory's instance is the exact reference returned by the fallback. Subclassing
    // DefaultStrokedCircleRenderable means we inherit the full IStrokedCircleRenderable +
    // IRenderable surface without re-implementing it for the test.
    private sealed class RegistryStrokedCircleSentinel : DefaultStrokedCircleRenderable { }

    private sealed class RegistryStrokedRectangleSentinel : DefaultStrokedRectangleRenderable { }
}
