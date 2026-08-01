using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;
using System.Windows.Forms;
using TextureCoordinateSelectionPlugin.Logic;

namespace GumToolUnitTests.Plugins.TextureCoordinateSelectionPlugin;

public class TextureCoordinateDisplayControllerTests
{
    private readonly Mock<IHotkeyManager> _hotkeyManagerMock;
    private readonly TextureCoordinateDisplayController _sut;

    public TextureCoordinateDisplayControllerTests()
    {
        _hotkeyManagerMock = new Mock<IHotkeyManager>();

        _sut = new TextureCoordinateDisplayController(
            new Mock<ISelectedState>().Object,
            new Mock<IUndoManager>().Object,
            new Mock<IGuiCommands>().Object,
            new Mock<IFileCommands>().Object,
            new Mock<ISetVariableLogic>().Object,
            new Mock<ITabManager>().Object,
            _hotkeyManagerMock.Object,
            new ScrollBarLogicWpf(new ScrollBarLogic()),
            new Mock<IMessenger>().Object,
            new Mock<IThemingService>().Object);
    }

    [Fact]
    public void HandleKeyDown_WhenAppWideHotkeyHandlesTheKey_ReturnsWithoutTouchingCamera()
    {
        // CreateControl() was never called, so the controller's inner WinForms/XNA control is
        // uninitialized. If HandleKeyDown fell through to the camera-pan logic below the app-wide
        // check, it would NullReferenceException here - reaching the assertion instead pins that
        // an app-wide-handled key (e.g. Ctrl+Z) returns before that point.
        _hotkeyManagerMock
            .Setup(m => m.PreviewKeyDownAppWide(It.IsAny<GumKeyEventArgs>(), false))
            .Callback<GumKeyEventArgs, bool>((args, _) => args.Handled = true)
            .Returns(true);
        var e = new KeyEventArgs(Keys.Control | Keys.Z);

        _sut.HandleKeyDown(null, e);

        e.Handled.ShouldBeTrue();
        _hotkeyManagerMock.Verify(m => m.PreviewKeyDownAppWide(It.IsAny<GumKeyEventArgs>(), false), Times.Once);
    }
}
