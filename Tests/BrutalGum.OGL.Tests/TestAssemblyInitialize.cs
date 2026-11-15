using Gum;
using SkiaHostTests;
using SkiaSharp;
using Xunit.Abstractions;

[assembly: Xunit.TestFramework("BrutalGum.Tests.TestAssemblyInitialize", "BrutalGum.OGL.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace BrutalGum.Tests;

/// <summary>
/// Assembly-wide bootstrap: the shared <see cref="SkiaHostTestFramework"/> with a fake window that reports
/// nothing. Forms-input tests add their own.
/// </summary>
public class TestAssemblyInitialize : SkiaHostTestFramework
{
    /// <summary>The fake window the shared test service reads, for tests that script a gamepad.</summary>
    internal static FakeBrutalInput Input { get; } = new();

    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
    }

    protected override void InitializeGum(SKCanvas canvas) =>
        GumService.Default.Initialize(canvas, Input, CanvasWidth, CanvasHeight);
}
