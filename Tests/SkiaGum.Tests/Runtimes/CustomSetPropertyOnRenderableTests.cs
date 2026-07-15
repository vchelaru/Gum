using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using SkiaGum.GueDeriving;
using SkiaSharp;

namespace SkiaGum.Tests.Runtimes;

// Covers the AdditionalPropertyOnRenderable extension hook on SkiaGum's SetPropertyOnRenderable
// dispatch (issue #3650 file-unification convergence). The unified MonoGame/Raylib copy checks this
// hook before falling back to reflection; SkiaGum's copy had dropped it entirely, so a plugin
// registering AdditionalPropertyOnRenderable (e.g. the Apos.Shapes runtimes) would silently never
// run under SkiaGum.
public class CustomSetPropertyOnRenderableTests
{
    public CustomSetPropertyOnRenderableTests()
    {
        // Wire up the SkiaGum custom property setter so SetProperty routes correctly.
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void SetPropertyOnRenderable_UnhandledProperty_ShouldInvokeAdditionalPropertyOnRenderable()
    {
        ContainerRuntime container = new();
        IRenderableIpso renderable = (IRenderableIpso)container.RenderableComponent;
        bool wasCalled = false;

        CustomSetPropertyOnRenderable.AdditionalPropertyOnRenderable = (ipso, gue, propertyName, value) =>
        {
            wasCalled = true;
            return true;
        };
        try
        {
            CustomSetPropertyOnRenderable.SetPropertyOnRenderable(renderable, container, "ThisPropertyDoesntExistAnywhere", 42);
        }
        finally
        {
            CustomSetPropertyOnRenderable.AdditionalPropertyOnRenderable = null;
        }

        wasCalled.ShouldBeTrue();
    }

    [Fact]
    public void SetPropertyOnRenderable_Typeface_ShouldForwardToRenderable()
    {
        // Typeface (#3708): SkiaGum's SetProperty dispatch had no arm for this at all -- it never
        // existed on Skia before, so the string-based path (codegen/state application) had nothing
        // to route to. Mirrors the MonoGame/Raylib Font/BitmapFont dispatch coverage.
        Gum.GueDeriving.TextRuntime textRuntime = new();
        IRenderableIpso renderable = (IRenderableIpso)textRuntime.RenderableComponent;
        SKTypeface typeface = SKTypeface.FromFamilyName("Arial");

        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(renderable, textRuntime, "Typeface", typeface);

        ((Text)renderable).Typeface.ShouldBe(typeface);
    }
}
