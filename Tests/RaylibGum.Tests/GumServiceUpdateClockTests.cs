using Gum.GueDeriving;
using Gum.Graphics.Animation;
using Gum.Renderables;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests;

/// <summary>
/// The clock behind <c>GumService.Update(double)</c>: the first Update after every Initialize advances
/// by zero, like the XNA-like host (#6347). Each test starts from a fresh Initialize so the shared
/// default service's earlier Updates cannot leak in.
/// </summary>
public class GumServiceUpdateClockTests : IDisposable
{
    private readonly ContainerRuntime _root = new();
    private readonly Sprite _sprite = new();

    // Held across Uninitialize: a game that keeps its reference re-initializes the same instance.
    private readonly GumService _service = GumService.Default;

    public GumServiceUpdateClockTests()
    {
        AnimationChain chain = new() { Name = "Chain" };
        chain.Add(new AnimationFrame { FrameLength = 1000 });
        AnimationChainList chains = new();
        chains.Add(chain);
        _sprite.AnimationLogic.AnimationChains = chains;
        _sprite.Animate = true;
        _root.AddChild(new GraphicalUiElement(_sprite));

        Reinitialize();
    }

    public void Dispose()
    {
        TestAssemblyInitialize.ApplyDefaultTestState();
    }

    // Uninitialize is a no-op on a service that is not initialized (#6066), so initialize first.
    private void Reinitialize()
    {
        _service.InitializeForTesting();
        _service.Uninitialize();

        // Uninitialize removes the cursor and keyboard that Update ticks; the bootstrap puts them back.
        TestAssemblyInitialize.ApplyDefaultTestState();
        _service.InitializeForTesting();
    }

    private double AnimatedSeconds => _sprite.AnimationLogic.TimeIntoAnimation;

    [Fact]
    public void Update_FirstFrameAfterInitialize_AdvancesByZero()
    {
        _service.Update(100.0, _root);

        AnimatedSeconds.ShouldBe(0, tolerance: 1e-9);
    }

    [Fact]
    public void Update_SecondFrame_AdvancesByTheGapSinceTheFirst()
    {
        _service.Update(100.0, _root);
        _service.Update(100.5, _root);

        AnimatedSeconds.ShouldBe(0.5, tolerance: 1e-9);
    }

    [Fact]
    public void Update_FirstFrameAfterReinitialize_AdvancesByZeroWhenTheClockKeepsRunning()
    {
        _service.Update(50.0, _root);

        Reinitialize();
        _service.Update(50.016, _root);

        AnimatedSeconds.ShouldBe(0, tolerance: 1e-9);
    }

    [Fact]
    public void Update_FirstFrameAfterReinitialize_AdvancesByZeroWhenTheClockRestartsFromZero()
    {
        _service.Update(100.0, _root);

        Reinitialize();
        _service.Update(0.0, _root);
        _service.Update(0.25, _root);

        AnimatedSeconds.ShouldBe(0.25, tolerance: 1e-9);
    }

    [Fact]
    public void Uninitialize_ClearsTheRecordedGameTime()
    {
        _service.Update(7.0, _root);
        ((IGumService)_service).GameTime.ShouldBe(7f);

        Reinitialize();

        ((IGumService)_service).GameTime.ShouldBeNull();
    }

    // These two use a root with nothing to animate: a NaN or astronomically large frame length fed to a
    // running animation is not something the test should depend on surviving.
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Update_WithANonFiniteClock_KeepsThePreviousClock(double totalSeconds)
    {
        ContainerRuntime emptyRoot = new();
        _service.Update(10.0, emptyRoot);

        _service.Update(totalSeconds, emptyRoot);

        ((IGumService)_service).GameTime.ShouldBe(10f);
    }

    [Fact]
    public void Update_WithAHugeClock_ClampsInsteadOfThrowing()
    {
        ContainerRuntime emptyRoot = new();
        _service.Update(10.0, emptyRoot);

        Should.NotThrow(() => _service.Update(1e300, emptyRoot));

        _service.GameTime.ShouldBe(4e11, tolerance: 1);
    }
}
