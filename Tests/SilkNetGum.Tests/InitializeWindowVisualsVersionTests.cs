using Gum;
using Gum.Forms;
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
}
