using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace SkiaHostTests;

/// <summary>
/// A host's fake keyboard and mouse devices, and the way they reach the service. A fresh rig is made
/// for each test.
/// </summary>
public interface IHostInputRig
{
    /// <summary>
    /// Initializes the assembly-wide service over these devices. Calling it again is a second Initialize
    /// over the same devices.
    /// </summary>
    void Initialize();

    /// <summary>The keyboard device raises typed <paramref name="text"/>.</summary>
    void Type(string text);

    /// <summary>The mouse device scrolls <paramref name="notches"/> wheel notches.</summary>
    void Scroll(float notches);
}

/// <summary>
/// The keyboard and cursor subscribe to the host's input devices, and the devices outlive the service,
/// so Uninitialize and a second Initialize must unsubscribe them. A host's test project derives from this
/// class and supplies the fake devices; the cases are shared. These tests re-initialize the assembly-wide
/// service, so each one puts it back through <see cref="RestoreSharedService"/>.
/// </summary>
public abstract class InputSubscriptionTestsBase
{
    private static GumServiceSkiaBase Service => (GumServiceSkiaBase)IGumService.Default!;

    /// <summary>A rig with a fresh keyboard and mouse.</summary>
    protected abstract IHostInputRig CreateRig();

    /// <summary>
    /// Uninitializes whatever is live and initializes the host's assembly-wide service again, leaving the
    /// shared state as the assembly bootstrap does.
    /// </summary>
    protected abstract void RestoreSharedService();

    private void RestoreAll()
    {
        RestoreSharedService();
        SkiaHostTestBase.CaptureRenderableBaseline();
        FrameworkElement.KeyboardsForUiControl.Clear();
    }

    [Fact]
    public void Uninitialize_StopsTheOldKeyboardReceivingTypedText()
    {
        IHostInputRig rig = CreateRig();
        try
        {
            Service.Uninitialize();
            rig.Initialize();
            IInputReceiverKeyboard oldKeyboard = IGumService.Default!.Keyboard!;

            Service.Uninitialize();
            rig.Type("x");
            oldKeyboard.Activity(0);

            oldKeyboard.GetStringTyped().ShouldBe("");
        }
        finally
        {
            RestoreAll();
        }
    }

    [Fact]
    public void Uninitialize_StopsTheOldCursorReceivingScroll()
    {
        IHostInputRig rig = CreateRig();
        try
        {
            Service.Uninitialize();
            rig.Initialize();
            ICursor oldCursor = IGumService.Default!.Cursor;

            Service.Uninitialize();
            rig.Scroll(2);
            oldCursor.Activity(0);

            oldCursor.ScrollWheelChange.ShouldBe(0);
        }
        finally
        {
            RestoreAll();
        }
    }

    [Fact]
    public void SecondInitialize_StopsThePreviousKeyboardReceivingTypedText()
    {
        IHostInputRig rig = CreateRig();
        try
        {
            Service.Uninitialize();
            rig.Initialize();
            IInputReceiverKeyboard firstKeyboard = IGumService.Default!.Keyboard!;

            rig.Initialize();
            rig.Type("x");
            firstKeyboard.Activity(0);

            firstKeyboard.GetStringTyped().ShouldBe("");
        }
        finally
        {
            RestoreAll();
        }
    }

    [Fact]
    public void LiveCursorAndKeyboard_ReceiveTheEvents()
    {
        // Guards the tests above: with the service still initialized, the same events do arrive.
        IHostInputRig rig = CreateRig();
        try
        {
            Service.Uninitialize();
            rig.Initialize();

            rig.Type("x");
            rig.Scroll(2);
            IGumService.Default!.Keyboard!.Activity(0);
            IGumService.Default.Cursor.Activity(0);

            IGumService.Default.Keyboard!.GetStringTyped().ShouldBe("x");
            IGumService.Default.Cursor.ScrollWheelChange.ShouldBe(2);
        }
        finally
        {
            RestoreAll();
        }
    }

    [Fact]
    public void Uninitialize_DoesNotDisposeACustomKeyboard()
    {
        Mock<IInputReceiverKeyboard> custom = new Mock<IInputReceiverKeyboard>();
        Mock<IDisposable> disposable = custom.As<IDisposable>();
        IHostInputRig rig = CreateRig();
        try
        {
            Service.Uninitialize();
            rig.Initialize();
            FormsUtilities.SetKeyboard(custom.Object);

            Service.Uninitialize();

            disposable.Verify(d => d.Dispose(), Times.Never());
        }
        finally
        {
            RestoreAll();
        }
    }
}
