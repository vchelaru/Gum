using Gum.GueDeriving;
using Shouldly;
using SokolGum;

namespace SokolGum.Tests;

/// <summary>
/// Narrow tests for <see cref="GumService"/>'s purely-managed surface.
///
/// <see cref="GumService.Initialize"/> calls <c>SystemManagers.Initialize</c>
/// which hits native sokol_gfx (<c>sg_make_sampler</c> etc.) without a
/// preceding <c>sg_setup</c> — in a unit-test context with no window that
/// crashes the test host, not just throws. So we don't cover that path
/// here: the two Sokol sample programs serve as the integration coverage
/// for Initialize, Update, Draw, and the Root-on-layer wiring.
///
/// What IS covered: singleton identity, the pre-init guard on AddToRoot,
/// and RemoveFromRoot's parent-clearing contract (which only needs a
/// live parent/child pair — no managers).
/// </summary>
public class GumServiceTests : BaseTestClass
{
    [Fact]
    public void Default_ShouldBeSingleton()
    {
        GumService.Default.ShouldBeSameAs(GumService.Default);
    }

    [Fact]
    public void AddToRoot_WhenNotInitialized_ShouldThrow()
    {
        // The fresh singleton in a non-init'd test process has
        // IsInitialized=false. The extension guards against this to give
        // users a clearer error than NullReferenceException on Root.
        var element = new ContainerRuntime();
        Should.Throw<InvalidOperationException>(() => element.AddToRoot());
    }

    [Fact]
    public void RemoveFromRoot_ShouldClearParent()
    {
        // Standalone test of the extension's contract — doesn't involve
        // GumService at all, since RemoveFromRoot is just "set Parent to
        // null" regardless of where the element was attached.
        var parent = new ContainerRuntime();
        var child = new ContainerRuntime();
        parent.Children.Add(child);
        child.Parent.ShouldBeSameAs(parent);

        child.RemoveFromRoot();

        child.Parent.ShouldBeNull();
    }
}
