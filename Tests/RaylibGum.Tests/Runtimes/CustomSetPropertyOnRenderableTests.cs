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

    // Issue #3706/#3708/#3710 — TrySetPropertyOnText's "MaxLettersToShow" branch was gated
    // #if XNALIKE only, despite TextRuntime.MaxLettersToShow being a platform-neutral property
    // that has worked on every backend via direct C# calls since #3708/#3710. Pins the explicit
    // redispatch now that the needless gate is removed.
    [Fact]
    public void SetProperty_MaxLettersToShow_ShouldForwardToTextRuntime()
    {
        TextRuntime sut = new();

        sut.SetProperty("MaxLettersToShow", (int?)5);

        sut.MaxLettersToShow.ShouldBe(5);
    }

    // Issue #3706/#3724 follow-up — TrySetPropertyOnText's "Color" branch was #if FRB / #elif
    // XNALIKE with no RAYLIB arm at all, so the branch body didn't compile in on raylib and the
    // string-path assignment silently did nothing (not even a reflection fallback, since
    // "handled" only gates whether SetPropertyOnRenderable falls through to reflection for the
    // property it was given — reflection can't find "Color" on TextRuntime either, since the
    // combined Color property is FRB/XNALIKE-only there too).
    [Fact]
    public void SetProperty_Color_ShouldForwardToTextRuntime()
    {
        TextRuntime sut = new();
        System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(10, 20, 30, 40);

        sut.SetProperty("Color", drawingColor);

        sut.Color.R.ShouldBe((byte)20);
        sut.Color.G.ShouldBe((byte)30);
        sut.Color.B.ShouldBe((byte)40);
        sut.Color.A.ShouldBe((byte)10);
    }
}
