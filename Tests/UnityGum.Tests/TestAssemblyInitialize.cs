using Gum;
using SkiaHostTests;
using SkiaSharp;
using Xunit.Abstractions;

[assembly: Xunit.TestFramework("UnityGum.Tests.TestAssemblyInitialize", "UnityGum.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace UnityGum.Tests;

/// <summary>
/// Assembly-wide bootstrap. Initializes the Unity <see cref="GumService"/> once against the shared
/// in-memory raster <see cref="SKSurface"/>, the same canvas the Unity package's CPU fallback draws into.
/// </summary>
public class TestAssemblyInitialize : SkiaHostTestFramework
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
    }

    protected override void InitializeGum(SKCanvas canvas) =>
        GumService.Default.Initialize(canvas, CanvasWidth, CanvasHeight);
}
