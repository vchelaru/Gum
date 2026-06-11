using Gum.GueDeriving;
using MonoGameAndGum.Renderables;
using MonoGameGum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #3112 — a plain RectangleRuntime / CircleRuntime must NOT require the Apos.Shapes
// ShapeRenderer. The MonoGameGumShapes package registers Apos slot-override factories for the
// fill/stroke slots, and GumService.Initialize's reflection scan force-registers them on WASM
// whenever the shapes assembly is merely loaded. If those factories handed out an Apos shape
// before ShapeRenderer was initialized, the rectangle/circle would throw "ShapeRenderer is null"
// at draw (the reported browser/KNI crash) even though the user never opted into shapes.
//
// The fix gates the four slot-override factories on ShapeRenderer.IsInitialized: until shapes is
// actually initialized they return null, so the runtime falls back to core's no-shapes default
// renderable — identical to desktop behavior when shapes was never set up.
//
// This class deliberately does NOT mark the ShapeRenderer initialized (mirroring the user's
// scenario), so it must run with IsInitialized == false. It resets the flag defensively in case
// another test in the run left a real/forced ShapeRenderer initialized.
public class ShapeRendererGateTests
{
    public ShapeRendererGateTests()
    {
        AposShapeRuntime.RegisterRuntimeTypes();
        ShapeRenderer.Self.SetIsInitializedForTesting(false);
    }

    [Fact]
    public void CircleRuntime_WhenShapeRendererNotInitialized_FallsBackToCoreDefault()
    {
        CircleRuntime sut = new();

        // Fill factory returns null (gated), so the stroke slot becomes the contained renderable:
        // core's DefaultStrokedCircleRenderable, not an Apos Circle.
        sut.RenderableComponent.ShouldBeOfType<DefaultStrokedCircleRenderable>();
    }

    [Fact]
    public void RectangleRuntime_WhenShapeRendererNotInitialized_FallsBackToCoreDefault()
    {
        RectangleRuntime sut = new();

        // Fill slot falls back to core's DefaultFilledRectangleRenderable (wraps SolidRectangle),
        // not an Apos RoundedRectangle.
        sut.RenderableComponent.ShouldBeOfType<DefaultFilledRectangleRenderable>();
    }
}
