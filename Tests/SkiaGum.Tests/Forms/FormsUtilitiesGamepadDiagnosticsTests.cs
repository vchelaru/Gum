using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;

namespace SkiaGum.Tests.Forms;

/// <summary>
/// The Skia build defines FULL_DIAGNOSTICS, under which <see cref="FormsUtilities.Update"/> rejects a
/// gamepad added to <see cref="FrameworkElement.GamePadsForUiControl"/> more than once.
/// </summary>
public class FormsUtilitiesGamepadDiagnosticsTests : BaseTestClass
{
    private readonly ICursor _previousCursor = FrameworkElement.MainCursor;
    private readonly IInputReceiverKeyboard _previousKeyboard = FrameworkElement.MainKeyboard;
    private readonly SkiaGum.Tests.Performance.SkiaSceneHost _host = new();
    private readonly SkiaGum.Tests.Performance.FrameKeyboard _keyboard = new();

    public FormsUtilitiesGamepadDiagnosticsTests()
    {
        FrameworkElement.MainCursor = new SkiaGum.Tests.Performance.FrameCursor();
        FrameworkElement.MainKeyboard = _keyboard;
    }

    public override void Dispose()
    {
        FrameworkElement.GamePadsForUiControl.Clear();
        FrameworkElement.MainCursor = _previousCursor;
        FrameworkElement.MainKeyboard = _previousKeyboard;
        _keyboard.Dispose();
        _host.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Update_SameGamepadAddedTwice_ShouldThrow()
    {
        GamePad gamepad = new();
        FrameworkElement.GamePadsForUiControl.Add(gamepad);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        InvalidOperationException exception =
            Should.Throw<InvalidOperationException>(() => FormsUtilities.Update(1, GumService.Default.Root));
        exception.Message.ShouldContain("multiple times");
    }

    [Fact]
    public void Update_DistinctGamepads_ShouldNotThrow()
    {
        FrameworkElement.GamePadsForUiControl.Add(new GamePad());
        FrameworkElement.GamePadsForUiControl.Add(new GamePad());

        Should.NotThrow(() => FormsUtilities.Update(1, GumService.Default.Root));
    }
}
