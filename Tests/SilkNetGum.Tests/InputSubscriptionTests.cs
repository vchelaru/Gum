using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Moq;
using Shouldly;
using Silk.NET.Input;
using SkiaHostTests;
using SkiaSharp;
using System.Collections.Generic;

namespace SilkNetGum.Tests;

/// <summary>
/// The keyboard and cursor subscribe to the Silk devices' events, and the devices outlive the
/// service, so Uninitialize and a second Initialize must unsubscribe them. These tests re-initialize
/// the assembly-wide service, so each one puts it back.
/// </summary>
public class InputSubscriptionTests : BaseTestClass
{
    private static Mock<IInputContext> CreateContext(Mock<IKeyboard> keyboard, Mock<IMouse> mouse)
    {
        mouse.SetupGet(m => m.ScrollWheels).Returns(new List<ScrollWheel>());

        Mock<IInputContext> context = new Mock<IInputContext>();
        context.SetupGet(c => c.Keyboards).Returns(new List<IKeyboard> { keyboard.Object });
        context.SetupGet(c => c.Mice).Returns(new List<IMouse> { mouse.Object });
        context.SetupGet(c => c.Gamepads).Returns(new List<IGamepad>());
        return context;
    }

    private static void Initialize(IInputContext context)
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
        GumService.Default.Initialize(surface.Canvas, context, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
    }

    private static void RestoreSharedService()
    {
        GumService.Default.Uninitialize();
        Initialize(CreateContext(new Mock<IKeyboard>(), new Mock<IMouse>()).Object);
        SkiaHostTestBase.CaptureRenderableBaseline();
        FrameworkElement.KeyboardsForUiControl.Clear();
    }

    [Fact]
    public void Uninitialize_StopsTheOldKeyboardReceivingTypedText()
    {
        Mock<IKeyboard> device = new Mock<IKeyboard>();
        try
        {
            GumService.Default.Uninitialize();
            Initialize(CreateContext(device, new Mock<IMouse>()).Object);
            Keyboard oldKeyboard = GumService.Default.Keyboard;

            GumService.Default.Uninitialize();
            device.Raise(k => k.KeyChar += null, device.Object, 'x');
            oldKeyboard.Activity(0);

            oldKeyboard.GetStringTyped().ShouldBe("");
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Uninitialize_StopsTheOldCursorReceivingScroll()
    {
        Mock<IMouse> mouse = new Mock<IMouse>();
        try
        {
            GumService.Default.Uninitialize();
            Initialize(CreateContext(new Mock<IKeyboard>(), mouse).Object);
            Cursor oldCursor = GumService.Default.Cursor;

            GumService.Default.Uninitialize();
            mouse.Raise(m => m.Scroll += null, mouse.Object, new ScrollWheel(0, 2));
            oldCursor.Activity(0);

            oldCursor.ScrollWheelChange.ShouldBe(0);
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void SecondInitialize_StopsThePreviousKeyboardReceivingTypedText()
    {
        Mock<IKeyboard> device = new Mock<IKeyboard>();
        try
        {
            GumService.Default.Uninitialize();
            Initialize(CreateContext(device, new Mock<IMouse>()).Object);
            Keyboard firstKeyboard = GumService.Default.Keyboard;

            Initialize(CreateContext(device, new Mock<IMouse>()).Object);
            device.Raise(k => k.KeyChar += null, device.Object, 'x');
            firstKeyboard.Activity(0);

            firstKeyboard.GetStringTyped().ShouldBe("");
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Uninitialize_DoesNotDisposeACustomKeyboard()
    {
        Mock<Gum.Wireframe.IInputReceiverKeyboard> custom = new Mock<Gum.Wireframe.IInputReceiverKeyboard>();
        Mock<System.IDisposable> disposable = custom.As<System.IDisposable>();
        try
        {
            GumService.Default.Uninitialize();
            Initialize(CreateContext(new Mock<IKeyboard>(), new Mock<IMouse>()).Object);
            FormsUtilities.SetKeyboard(custom.Object);

            GumService.Default.Uninitialize();

            disposable.Verify(d => d.Dispose(), Times.Never());
        }
        finally
        {
            RestoreSharedService();
        }
    }
}
