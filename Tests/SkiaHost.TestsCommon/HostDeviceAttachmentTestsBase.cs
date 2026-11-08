using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace SkiaHostTests;

/// <summary>
/// Pins that the interactive base owns the Initialize order: a host hands it an attach callback, and
/// the base runs that before it creates the cursor and keyboard. The test host assigns nothing before
/// calling the base, so a base that did not own the order would create devices with no state.
/// A host's test project derives from this class and supplies <see cref="RestoreSharedService"/>,
/// because these tests initialize a second service over the assembly-wide one.
/// </summary>
public abstract class HostDeviceAttachmentTestsBase : IDisposable
{
    private sealed class TestHost : GumServiceSkiaInteractiveBase, IGumService
    {
        public readonly List<string> Events = new();
        public string? DeviceStateSeenByCursor;
        public string? DeviceStateSeenByKeyboard;
        public string? DeviceState;
        public int ClipboardSeenByCursor = -1;
        public IGumClipboard? ClipboardToAssign;

        public void Initialize(SKCanvas canvas, string deviceState, DefaultVisualsVersion visualsVersion)
        {
            InitializeWithDevices(canvas, 100, 100, null, () =>
            {
                Events.Add("attach");
                DeviceState = deviceState;
                Clipboard = ClipboardToAssign;
            }, visualsVersion);
        }

        ICursor? IGumService.CreateCursor()
        {
            Events.Add("cursor");
            DeviceStateSeenByCursor = DeviceState;
            ClipboardSeenByCursor = Clipboard == null ? 0 : 1;
            return OwnInput(new DisposableCursor()).Cursor;
        }

        IInputReceiverKeyboard? IGumService.CreateKeyboard()
        {
            Events.Add("keyboard");
            DeviceStateSeenByKeyboard = DeviceState;
            return OwnInput(new DisposableKeyboard()).Keyboard;
        }
    }

    private sealed class DisposableCursor : IDisposable
    {
        public ICursor Cursor { get; } = new Mock<ICursor>().Object;
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    private sealed class DisposableKeyboard : IDisposable
    {
        public IInputReceiverKeyboard Keyboard { get; } = new Mock<IInputReceiverKeyboard>().Object;
        public void Dispose() { }
    }

    /// <summary>
    /// Uninitializes whatever is live and initializes the host's assembly-wide service again, leaving
    /// the shared state as the assembly bootstrap does.
    /// </summary>
    protected abstract void RestoreSharedService();

    public void Dispose() => RestoreSharedService();

    private static SKCanvas CreateCanvas() => SKSurface.Create(new SKImageInfo(100, 100)).Canvas;

    [Fact]
    public void Initialize_AttachesDevicesBeforeCreatingCursorAndKeyboard()
    {
        TestHost host = new TestHost();
        Mock<IGumClipboard> clipboard = new Mock<IGumClipboard>();
        host.ClipboardToAssign = clipboard.Object;

        host.Initialize(CreateCanvas(), "devices", DefaultVisualsVersion.V3);

        host.Events.IndexOf("attach").ShouldBe(0);
        host.Events.ShouldContain("cursor");
        host.Events.ShouldContain("keyboard");
        host.DeviceStateSeenByCursor.ShouldBe("devices");
        host.DeviceStateSeenByKeyboard.ShouldBe("devices");
        host.ClipboardSeenByCursor.ShouldBe(1);
    }

    [Fact]
    public void Initialize_WithUnsupportedVisualsVersion_DoesNotAttachDevices()
    {
        TestHost host = new TestHost();

        Should.Throw<NotSupportedException>(() =>
            host.Initialize(CreateCanvas(), "devices", (DefaultVisualsVersion)99));

        host.Events.ShouldBeEmpty();
        host.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    public void Initialize_Twice_AttachesAgainBeforeEachCursor()
    {
        TestHost host = new TestHost();

        host.Initialize(CreateCanvas(), "first", DefaultVisualsVersion.V3);
        host.Initialize(CreateCanvas(), "second", DefaultVisualsVersion.V3);

        host.DeviceStateSeenByCursor.ShouldBe("second");
        host.DeviceStateSeenByKeyboard.ShouldBe("second");
    }
}
