using Gum;
using SkiaHostTests;
using SkiaSharp;
using Stride.Input;
using Xunit.Abstractions;

[assembly: Xunit.TestFramework("StrideGum.Tests.TestAssemblyInitialize", "StrideGum.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace StrideGum.Tests;

/// <summary>
/// Assembly-wide test bootstrap for StrideGum: the shared <see cref="SkiaHostTestFramework"/> with no
/// real window or GraphicsDevice. It calls <see cref="GumService.InitializeCore"/> (the Gum-facing half
/// of <c>Initialize(Game, ...)</c> that needs no live Stride <c>GraphicsDevice</c>/<c>Texture</c>/
/// <c>GumSceneRenderer</c>), not the public overload, because a real Stride <c>Game</c> isn't
/// constructible in a unit test. A bare, never-<c>.Initialize(GameContext)</c>d <see cref="InputManager"/>
/// degrades safely (no keyboards, mice or gamepads registered), and Forms-input tests attach their own
/// mocked devices directly.
/// </summary>
public class TestAssemblyInitialize : SkiaHostTestFramework
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
    }

    protected override void InitializeGum(SKCanvas canvas)
    {
        var inputManager = new InputManager();

        GumService.Default.InitializeCore(canvas, inputManager, CanvasWidth, CanvasHeight, null);
    }
}
