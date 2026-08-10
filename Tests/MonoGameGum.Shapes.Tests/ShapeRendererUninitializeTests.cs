using MonoGameAndGum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #4416 — GumService.Uninitialize() didn't reset ShapeRenderer.Self, so a second
// Initialize() threw "ShapeRenderer is already initialized" (e.g. FlatRedBall2's startup path).
// These tests exercise ShapeRenderer.Uninitialize() headlessly via the test-only IsInitialized
// seam; the real GraphicsDevice-backed round trip is covered by
// MonoGameGum.IntegrationTests.MonoGameGum.UninitializeTests.
public class ShapeRendererUninitializeTests
{
    [Fact]
    public void Uninitialize_WhenInitialized_ResetsIsInitializedToFalse()
    {
        ShapeRenderer.Self.SetIsInitializedForTesting(true);

        ShapeRenderer.Self.Uninitialize();

        ShapeRenderer.Self.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    public void Uninitialize_WhenNeverInitialized_DoesNotThrow()
    {
        ShapeRenderer.Self.SetIsInitializedForTesting(false);

        Should.NotThrow(() => ShapeRenderer.Self.Uninitialize());
    }
}
