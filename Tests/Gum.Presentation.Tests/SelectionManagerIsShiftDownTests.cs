using Gum.Commands;
using Gum.Input;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="SelectionManager.IsShiftDown"/> after its relocation to Gum.Presentation
/// (ADR-0005, part of #3846) — it previously read the live modifier state directly via WinForms
/// <c>Control.ModifierKeys</c>, unreachable from headless code. It now delegates to
/// <see cref="IHotkeyManager.IsPressedInControl"/>, the same live-modifier-state seam already used
/// for the multi-select hotkey, with a Shift-only <see cref="KeyCombination"/>.
/// </summary>
public class SelectionManagerIsShiftDownTests
{
    private static (SelectionManager SelectionManager, Mock<IHotkeyManager> HotkeyManager) CreateSut()
    {
        var hotkeyManager = new Mock<IHotkeyManager>();

        var selectionManager = new SelectionManager(
            Mock.Of<ISelectedState>(),
            Mock.Of<IUndoManager>(),
            Mock.Of<IContextMenuState>(),
            Mock.Of<Gum.Services.Dialogs.IDialogService>(),
            hotkeyManager.Object,
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IWireframeEditorFactory>(),
            Mock.Of<INineSliceCoordinateRefresher>(),
            Mock.Of<IPreciseHitTester>());

        return (selectionManager, hotkeyManager);
    }

    [Fact]
    public void IsShiftDown_ReturnsTrue_WhenHotkeyManagerReportsShiftHeld()
    {
        var (selectionManager, hotkeyManager) = CreateSut();
        hotkeyManager
            .Setup(h => h.IsPressedInControl(It.Is<KeyCombination>(c => c.IsShiftDown && c.Key == null)))
            .Returns(true);

        selectionManager.IsShiftDown.ShouldBeTrue();
    }

    [Fact]
    public void IsShiftDown_ReturnsFalse_WhenHotkeyManagerReportsShiftNotHeld()
    {
        var (selectionManager, hotkeyManager) = CreateSut();
        hotkeyManager
            .Setup(h => h.IsPressedInControl(It.Is<KeyCombination>(c => c.IsShiftDown && c.Key == null)))
            .Returns(false);

        selectionManager.IsShiftDown.ShouldBeFalse();
    }
}
