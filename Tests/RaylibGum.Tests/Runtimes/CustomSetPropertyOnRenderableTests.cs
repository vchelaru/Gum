using Gum.GueDeriving;
using Gum.Wireframe;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

// Covers the AdditionalPropertyOnRenderable extension hook on the shared
// SetPropertyOnRenderable dispatch (issue #3615 file-unification convergence). MonoGame/KNI/FNA
// and SkiaGum/SokolGum all check this hook before falling back to reflection; the raylib copy had
// dropped the check entirely, so a plugin registering AdditionalPropertyOnRenderable (e.g. the
// Apos.Shapes runtimes) would silently never run on raylib.
public class CustomSetPropertyOnRenderableTests : BaseTestClass
{
    [Fact]
    public void SetPropertyOnRenderable_UnhandledProperty_ShouldInvokeAdditionalPropertyOnRenderable()
    {
        var container = new ContainerRuntime();
        var renderable = (IRenderableIpso)container.RenderableComponent;
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
}
