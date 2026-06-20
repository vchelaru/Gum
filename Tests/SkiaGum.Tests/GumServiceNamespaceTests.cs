using Gum;
using RenderingLibrary;
using Shouldly;

namespace SkiaGum.Tests;

/// <summary>
/// Locks in the GumService host model (issue #3218): the render-only Skia service is
/// <c>Gum.GumService</c> (namespace <c>Gum</c>), mirroring the game-host MonoGameGum
/// service so user code is portable across hosts. It is no longer
/// <c>SkiaGum.GumService</c> — that type was evicted from the Gum.SkiaSharp core
/// rendering library with no [Obsolete] shim.
/// </summary>
public class GumServiceNamespaceTests
{
    [Fact]
    public void GumService_ShouldBeInGumNamespace()
    {
        typeof(GumService).Namespace.ShouldBe("Gum");
    }

    [Fact]
    public void GumService_ShouldImplementIGumService()
    {
        typeof(IGumService).IsAssignableFrom(typeof(GumService)).ShouldBeTrue();
    }
}
