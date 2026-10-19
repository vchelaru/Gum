using Gum;
using Gum.Input;
using SkiaHostTests;
using SkiaSharp;
using Xunit.Abstractions;

[assembly: Xunit.TestFramework("KniSkiaGum.Tests.TestAssemblyInitialize", "KniSkiaGum.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace KniSkiaGum.Tests;

/// <summary>
/// Assembly-wide test bootstrap for KniSkiaGum: the shared <see cref="SkiaHostTestFramework"/>. No KNI
/// <c>Game</c> exists in tests (it needs a platform package), so Gum is initialized without a window and
/// the input tests supply device state through the Cursor, Keyboard and GamePadDriver seams.
/// </summary>
public class TestAssemblyInitialize : SkiaHostTestFramework
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
    }

    protected override void InitializeGum(SKCanvas canvas)
    {
        // Initialize polls the gamepads once, and KNI's GamePad needs a platform package. No pad.
        GamePadDriver.ReadState = _ => default;

        GumService.Default.InitializeWithoutWindow(canvas, CanvasWidth, CanvasHeight);

        // Same for the mouse and keyboard, which Update polls: no device state in a test process.
        GumService.Default.Cursor.ReadMouse = () => default;
        GumService.Default.Keyboard.ReadKeyboard = () => default;
    }
}
