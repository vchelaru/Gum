using Gum;
using Gum.Forms.Controls;
using RenderingLibrary;
using SkiaSharp;
using Stride.Input;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("StrideGum.Tests.TestAssemblyInitialize", "StrideGum.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace StrideGum.Tests;

/// <summary>
/// Assembly-wide test bootstrap for StrideGum. Like SilkNetGum's bootstrap, this drives Gum through
/// an in-memory <see cref="SKSurface"/> with no real window/GraphicsDevice: it calls
/// <see cref="GumService.InitializeCore"/> (the Gum-facing half of <c>Initialize(Game, ...)</c> that
/// needs no live Stride <c>GraphicsDevice</c>/<c>Texture</c>/<c>GumSceneRenderer</c>), not the public
/// <c>Initialize(Game, ...)</c> overload -- a real Stride <c>Game</c>/<c>GraphicsDevice</c> isn't
/// constructible in a unit test. A bare, never-<c>.Initialize(GameContext)</c>d
/// <see cref="InputManager"/> is used for the same reason SilkNetGum's bootstrap uses an
/// <see cref="IInputContext"/> mock with no devices: it degrades safely (no keyboards/mice/gamepads
/// registered), and Forms-input tests attach their own mocked devices directly.
/// </summary>
public class TestAssemblyInitialize : XunitTestFramework
{
    // Kept alive for the whole run: the raster surface backs SystemManagers.Default.Canvas.
    private static SKSurface? _surface;

    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
        ApplyDefaultTestState();
    }

    /// <summary>
    /// Sets up the assembly-wide test state. Called once from the constructor.
    /// </summary>
    public static void ApplyDefaultTestState()
    {
        _surface = SKSurface.Create(new SKImageInfo(800, 600));

        // Never .Initialize(GameContext)'d, so it has no real Mouse/Keyboards/GamePads -- CreateCursor
        // yields a device-less cursor and CreateKeyboard yields a device-less Keyboard, which is fine
        // for tests (Forms-input tests attach their own mocked devices).
        var inputManager = new InputManager();

        GumService.Default.InitializeCore(_surface.Canvas, inputManager, 800, 600, null);

        // #3066 pattern: record post-bootstrap renderables so BaseTestClass.Dispose can sweep
        // anything a test leaks onto the shared layers, keeping tests order-independent.
        BaseTestClass.CaptureRenderableBaseline();

        // No real keyboard is registered (the bare InputManager has none); ensure the list is clean so
        // tests that Add a Mock<IInputReceiverKeyboard> are the sole registered keyboard.
        FrameworkElement.KeyboardsForUiControl.Clear();
    }
}
