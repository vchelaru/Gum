using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Silk.NET.Input;
using SkiaSharp;
using System.Collections.Generic;
using Moq;
using Shouldly;
using Silk.NET.Core.Contexts;
using Silk.NET.Windowing;

namespace SilkNetGum.Tests;

/// <summary>
/// Initialize(IWindow, DefaultVisualsVersion, string?) mirrors MonoGame's Initialize(Game, DefaultVisualsVersion).
/// Skia supports V3 only, so the version is checked before any window resource is created.
/// </summary>
public class InitializeWindowVisualsVersionTests : BaseTestClass
{
    // V3 is the only DefaultVisualsVersion left (V1 and V2 were removed), so an undefined value stands in for "not V3".
    private const DefaultVisualsVersion UnsupportedVersion = (DefaultVisualsVersion)99;

    private static Mock<IWindow> CreateWindow()
    {
        Mock<IGLContext> glContext = new Mock<IGLContext>();
        Mock<IWindow> window = new Mock<IWindow>();
        window.SetupGet(x => x.GLContext).Returns(glContext.Object);
        return window;
    }

    [Fact]
    public void Initialize_WindowWithV2_ThrowsNotSupported()
    {
        Mock<IWindow> window = CreateWindow();

        Should.Throw<NotSupportedException>(
            () => new GumService().Initialize(window.Object, UnsupportedVersion));

        // Rejected before the window was touched.
        window.VerifyGet(x => x.FramebufferSize, Times.Never);
    }

    [Fact]
    public void Initialize_WindowWithV3_IsNotRejectedAsUnsupported()
    {
        Mock<IWindow> window = CreateWindow();
        window.SetupGet(x => x.FramebufferSize).Throws(new InvalidOperationException("reached window"));

        // V3 passes the version check and proceeds to the window, which this mock stops.
        Should.Throw<InvalidOperationException>(
            () => new GumService().Initialize(window.Object, DefaultVisualsVersion.V3))
            .Message.ShouldBe("reached window");
    }

    [Fact]
    public void Initialize_DoesNotRegisterGamepadsUntilUseGamepadDefaults()
    {
        // The window overload needs a real GL context and a real window for CreateInput (an extension method),
        // so it cannot run headless. It ends in this overload; the window overload no longer adds anything.
        Mock<IInputContext> input = new Mock<IInputContext>();
        input.SetupGet(c => c.Keyboards).Returns(new List<IKeyboard> { new Mock<IKeyboard>().Object });
        Mock<IMouse> mouse = new Mock<IMouse>();
        mouse.SetupGet(m => m.ScrollWheels).Returns(new List<ScrollWheel>());
        input.SetupGet(c => c.Mice).Returns(new List<IMouse> { mouse.Object });
        input.SetupGet(c => c.Gamepads).Returns(new List<IGamepad>());
        GumService service = new GumService();
        FrameworkElement.GamePadsForUiControl.Clear();

        try
        {
            using SKSurface surface = SKSurface.Create(new SKImageInfo(800, 600));
            service.Initialize(surface.Canvas, input.Object, 800, 600);

            FrameworkElement.GamePadsForUiControl.ShouldBeEmpty();

            service.UseGamepadDefaults();

            FrameworkElement.GamePadsForUiControl.ShouldBe(service.Gamepads);
        }
        finally
        {
            FrameworkElement.GamePadsForUiControl.Clear();
        }
    }
}
