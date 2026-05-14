using Gum.Wireframe;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Wireframe;

/// <summary>
/// Unit tests for <see cref="RuntimeObjectCreator.TryHandleAsBaseType"/>, the factory that
/// maps a Gum standard element base type name to its backing <see cref="IRenderable"/>.
/// This type is compiled into every XNA-like runtime and the Gum tool, so a regression here
/// breaks both runtime rendering and the editor.
/// </summary>
public class RuntimeObjectCreatorTests : BaseTestClass
{
    public override void Dispose()
    {
        // TryHandleAsBaseType reads this static; restore the default so toggle tests don't leak.
        GraphicalUiElement.ShowLineRectangles = false;
        base.Dispose();
    }

    [Fact]
    public void TryHandleAsBaseType_Circle_ReturnsLineCircle()
    {
        IRenderable result = RuntimeObjectCreator.TryHandleAsBaseType("Circle", null);
        result.ShouldBeOfType<LineCircle>();
    }

    [Fact]
    public void TryHandleAsBaseType_ColoredRectangle_ReturnsSolidRectangle()
    {
        IRenderable result = RuntimeObjectCreator.TryHandleAsBaseType("ColoredRectangle", null);
        result.ShouldBeOfType<SolidRectangle>();
    }

    [Fact]
    public void TryHandleAsBaseType_Component_ReturnsNonNull_WhenShowLineRectanglesIsFalse()
    {
        // Regression guard for PR #2746: dropping the else branch made Component return null
        // whenever ShowLineRectangles was false (its default), breaking old/XML-error projects.
        GraphicalUiElement.ShowLineRectangles = false;

        IRenderable result = RuntimeObjectCreator.TryHandleAsBaseType("Component", null);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void TryHandleAsBaseType_Container_ReturnsLineRectangle_WhenShowLineRectanglesIsTrue()
    {
        GraphicalUiElement.ShowLineRectangles = true;

        IRenderable result = RuntimeObjectCreator.TryHandleAsBaseType("Container", null);

        result.ShouldBeOfType<LineRectangle>();
    }

    [Fact]
    public void TryHandleAsBaseType_Container_ReturnsNonNull_WhenShowLineRectanglesIsFalse()
    {
        // Regression guard for PR #2746: with ShowLineRectangles false (the default, and the
        // common case in the Gum tool), Container must still resolve to an InvisibleRenderable
        // rather than null. A null here leaves the GraphicalUiElement with no contained object.
        GraphicalUiElement.ShowLineRectangles = false;

        IRenderable result = RuntimeObjectCreator.TryHandleAsBaseType("Container", null);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void TryHandleAsBaseType_NineSlice_ReturnsNineSlice()
    {
        IRenderable result = RuntimeObjectCreator.TryHandleAsBaseType("NineSlice", null);
        result.ShouldBeOfType<NineSlice>();
    }

    [Fact]
    public void TryHandleAsBaseType_Polygon_ReturnsLinePolygon()
    {
        IRenderable result = RuntimeObjectCreator.TryHandleAsBaseType("Polygon", null);
        result.ShouldBeOfType<LinePolygon>();
    }

    [Fact]
    public void TryHandleAsBaseType_Rectangle_ReturnsLineRectangle()
    {
        IRenderable result = RuntimeObjectCreator.TryHandleAsBaseType("Rectangle", null);
        result.ShouldBeOfType<LineRectangle>();
    }

    [Fact]
    public void TryHandleAsBaseType_Sprite_ReturnsSprite()
    {
        IRenderable result = RuntimeObjectCreator.TryHandleAsBaseType("Sprite", null);
        result.ShouldBeOfType<Sprite>();
    }

    [Fact]
    public void TryHandleAsBaseType_Text_ReturnsText()
    {
        IRenderable result = RuntimeObjectCreator.TryHandleAsBaseType("Text", null);
        result.ShouldBeOfType<Text>();
    }

    [Fact]
    public void TryHandleAsBaseType_UnrecognizedType_ReturnsNull()
    {
        // A non-standard name (e.g. a custom component's own name) is expected to fall through;
        // ElementSaveExtensions.CreateGraphicalComponent relies on this null to recurse into base types.
        IRenderable result = RuntimeObjectCreator.TryHandleAsBaseType("SomeCustomComponent", null);
        result.ShouldBeNull();
    }
}
