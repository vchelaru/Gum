using System.Collections.Generic;
using System.Linq;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.IntegrationTests.MonoGameGum.Rendering.ManualRenderTarget;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Performance;

/// <summary>
/// A static scene of cached (<see cref="RenderTargetRefreshMode.Manual"/>) render-target containers allocates nothing
/// per frame in steady state, with and without an effect (#7624). Same ceiling and method as
/// <see cref="RenderTargetAllocationTests"/>.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class RenderTargetManualAllocationTests : BaseTestClass
{
    private const int ContainerCount = 1000;

    // Per container per frame, the per-frame fixed cost divided across the containers included.
    private const double MaxBytesPerContainerPerFrame = 8;

    private readonly ITestOutputHelper _output;

    public RenderTargetManualAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StaticManualContainers_AllocateNothingPerFrame(bool withEffect)
    {
        using ManualRenderTargetScene scene = new();
        List<ContainerRuntime> containers = new();
        for (int i = 0; i < ContainerCount; i++)
        {
            ContainerRuntime container = scene.CreateContainer(
                (i % 10) * 12, (i / 10 % 10) * 12, Color.Red, manual: true, withEffect, width: 12, height: 12);
            scene.Add(container);
            containers.Add(container);
        }

        scene.DrawFrames(3);
        int bakesBefore = scene.BakeCount;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () => scene.DrawFrame(),
            attempts: 3,
            warmupIterations: 10,
            measuredIterations: 30);

        double perContainer = result.BytesPerIteration / ContainerCount;
        _output.WriteLine($"{ContainerCount} manual containers, effect {withEffect}: {result.BytesPerIteration:N0} bytes/frame, " +
            $"{perContainer:N1} bytes per container per frame");

        scene.BakeCount.ShouldBe(bakesBefore, "steady state does not bake");
        perContainer.ShouldBeLessThanOrEqualTo(MaxBytesPerContainerPerFrame,
            $"measured {perContainer:N1} bytes per container per frame");
    }
}
