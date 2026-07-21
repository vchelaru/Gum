using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.SelectionHistory;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;

namespace GumToolUnitTests.Managers;

public class HotkeyManagerTests : BaseTestClass
{
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IElementCommands> _elementCommands;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<ISetVariableLogic> _setVariableLogic;
    private readonly Mock<IUiSettingsService> _uiSettingsService;
    private readonly Mock<ICopyPasteLogic> _copyPasteLogic;
    private readonly Mock<IUndoManager> _undoManager;
    private readonly Mock<IEditCommands> _editCommands;
    private readonly Mock<IReorderLogic> _reorderLogic;
    private readonly Mock<IPluginManager> _pluginManager;
    private readonly Mock<ISelectionHistory> _selectionHistory;
    private readonly HotkeyManager _hotkeyManager;

    public HotkeyManagerTests()
    {
        _guiCommands = new Mock<IGuiCommands>();
        _selectedState = new Mock<ISelectedState>();
        _elementCommands = new Mock<IElementCommands>();
        _dialogService = new Mock<IDialogService>();
        _fileCommands = new Mock<IFileCommands>();
        _setVariableLogic = new Mock<ISetVariableLogic>();
        _uiSettingsService = new Mock<IUiSettingsService>();
        _copyPasteLogic = new Mock<ICopyPasteLogic>();
        _undoManager = new Mock<IUndoManager>();
        _editCommands = new Mock<IEditCommands>();
        _reorderLogic = new Mock<IReorderLogic>();
        _pluginManager = new Mock<IPluginManager>();
        _selectionHistory = new Mock<ISelectionHistory>();

        _hotkeyManager = new HotkeyManager(
            _guiCommands.Object,
            _selectedState.Object,
            _elementCommands.Object,
            _dialogService.Object,
            _fileCommands.Object,
            _setVariableLogic.Object,
            _uiSettingsService.Object,
            _copyPasteLogic.Object,
            _undoManager.Object,
            _editCommands.Object,
            _reorderLogic.Object,
            _pluginManager.Object,
            _selectionHistory.Object
        );
    }

    [Fact]
    public void IsPressed_KeyData_NudgeUp5_RequiresShift()
    {
        // NudgeUp5 is Shift+Up: the Shift modifier bit is required.
        _hotkeyManager.NudgeUp5.IsPressed(System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Up).ShouldBeTrue();
        _hotkeyManager.NudgeUp5.IsPressed(System.Windows.Forms.Keys.Up).ShouldBeFalse();
    }

    [Fact]
    public void IsPressed_KeyData_NudgeUp_MatchesUp_AndIgnoresShiftBit()
    {
        // NudgeUp is a plain Up with no modifier requirement. The keyData flags overload must
        // strip modifier bits to extract the key code, so Shift+Up still matches the key part.
        _hotkeyManager.NudgeUp.IsPressed(System.Windows.Forms.Keys.Up).ShouldBeTrue();
        _hotkeyManager.NudgeUp.IsPressed(System.Windows.Forms.Keys.Down).ShouldBeFalse();
        _hotkeyManager.NudgeUp.IsPressed(System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Up).ShouldBeTrue();
    }

    [Fact]
    public void IsPressed_KeyData_ReorderUp_MatchesAltPlusKey()
    {
        // ReorderUp is Alt+Up: matches only when both the Alt flag and the Up key code are present.
        _hotkeyManager.ReorderUp.IsPressed(System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Up).ShouldBeTrue();
        _hotkeyManager.ReorderUp.IsPressed(System.Windows.Forms.Keys.Up).ShouldBeFalse();
    }

    [Fact]
    public void IsPressed_KeyData_ResizeFromCenter_MatchesAltOnly()
    {
        // ResizeFromCenter is Alt with no key: matches the bare Alt modifier, not Alt+other.
        _hotkeyManager.ResizeFromCenter.IsPressed(System.Windows.Forms.Keys.Alt).ShouldBeTrue();
        _hotkeyManager.ResizeFromCenter.IsPressed(System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.C).ShouldBeFalse();
    }

    [Fact]
    public void IsPressed_KeyEventArgs_Copy_RequiresCtrlPlusC()
    {
        _hotkeyManager.Copy.IsPressed(new System.Windows.Forms.KeyEventArgs(System.Windows.Forms.Keys.C | System.Windows.Forms.Keys.Control)).ShouldBeTrue();
        _hotkeyManager.Copy.IsPressed(new System.Windows.Forms.KeyEventArgs(System.Windows.Forms.Keys.C)).ShouldBeFalse();
    }

    [Fact]
    public void IsPressed_KeyEventArgs_Delete_MatchesDelete()
    {
        _hotkeyManager.Delete.IsPressed(new System.Windows.Forms.KeyEventArgs(System.Windows.Forms.Keys.Delete)).ShouldBeTrue();
        _hotkeyManager.Delete.IsPressed(new System.Windows.Forms.KeyEventArgs(System.Windows.Forms.Keys.C)).ShouldBeFalse();
    }

    [Fact]
    public void ProcessCmdKeyWireframe_Arrow_NudgesOnePixel_AndRaisesVariableSet()
    {
        (ComponentSave element, InstanceSave instance) = SetUpSelectedInstance(x: 10f, y: 20f);

        bool handled = _hotkeyManager.ProcessCmdKeyWireframe(GumKey.Up, isShiftDown: false, isCtrlDown: false, isAltDown: false);

        handled.ShouldBeTrue();
        _elementCommands.Verify(e => e.MoveSelectedObjectsBy(0f, -1f), Times.Once);
        _undoManager.Verify(u => u.RecordState(), Times.Once);
        // Only the moved axis (Y) raises VariableSet, carrying the pre-move value.
        _pluginManager.Verify(p => p.VariableSet(element, instance, "Y", 20f), Times.Once);
        _pluginManager.Verify(
            p => p.VariableSet(It.IsAny<ElementSave>(), It.IsAny<InstanceSave>(), "X", It.IsAny<object>()),
            Times.Never);
    }

    [Fact]
    public void ProcessCmdKeyWireframe_CtrlArrow_DoesNotNudge()
    {
        // Ctrl+arrow pans the camera elsewhere, so it must not also nudge the selection here.
        SetUpSelectedInstance(x: 10f, y: 20f);

        bool handled = _hotkeyManager.ProcessCmdKeyWireframe(GumKey.Up, isShiftDown: false, isCtrlDown: true, isAltDown: false);

        handled.ShouldBeFalse();
        _elementCommands.Verify(e => e.MoveSelectedObjectsBy(It.IsAny<float>(), It.IsAny<float>()), Times.Never);
    }

    [Fact]
    public void ProcessCmdKeyWireframe_LockedInstance_DoesNothing()
    {
        SetUpSelectedInstance(x: 10f, y: 20f, locked: true);

        bool handled = _hotkeyManager.ProcessCmdKeyWireframe(GumKey.Up, isShiftDown: false, isCtrlDown: false, isAltDown: false);

        handled.ShouldBeFalse();
        _elementCommands.Verify(e => e.MoveSelectedObjectsBy(It.IsAny<float>(), It.IsAny<float>()), Times.Never);
        _undoManager.Verify(u => u.RecordState(), Times.Never);
    }

    [Fact]
    public void ProcessCmdKeyWireframe_ShiftArrow_NudgesFivePixels()
    {
        SetUpSelectedInstance(x: 10f, y: 20f);

        bool handled = _hotkeyManager.ProcessCmdKeyWireframe(GumKey.Up, isShiftDown: true, isCtrlDown: false, isAltDown: false);

        handled.ShouldBeTrue();
        _elementCommands.Verify(e => e.MoveSelectedObjectsBy(0f, -5f), Times.Once);
    }

    [Fact]
    public void IsPressed_KeyData_NavigateBack_MatchesAltPlusLeft()
    {
        // NavigateBack is Alt+Left: matches only when both the Alt flag and the Left key code are present.
        _hotkeyManager.NavigateBack.IsPressed(System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Left).ShouldBeTrue();
        _hotkeyManager.NavigateBack.IsPressed(System.Windows.Forms.Keys.Left).ShouldBeFalse();
    }

    [Fact]
    public void IsPressed_KeyData_NavigateForward_MatchesAltPlusRight()
    {
        // NavigateForward is Alt+Right: matches only when both the Alt flag and the Right key code are present.
        _hotkeyManager.NavigateForward.IsPressed(System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Right).ShouldBeTrue();
        _hotkeyManager.NavigateForward.IsPressed(System.Windows.Forms.Keys.Right).ShouldBeFalse();
    }

    [Fact]
    public void ZoomCameraIn_KeyCombination_ShouldBeCtrlPlus()
    {
        // Verify zoom hotkey configuration
        _hotkeyManager.ZoomCameraIn.Key.ShouldBe(GumKey.Add);
        _hotkeyManager.ZoomCameraIn.IsCtrlDown.ShouldBeTrue();
    }

    [Fact]
    public void ZoomCameraInAlternative_KeyCombination_ShouldBeCtrlOemPlus()
    {
        // Verify alternative zoom hotkey configuration
        _hotkeyManager.ZoomCameraInAlternative.Key.ShouldBe(GumKey.Oemplus);
        _hotkeyManager.ZoomCameraInAlternative.IsCtrlDown.ShouldBeTrue();
    }

    [Fact]
    public void ZoomCameraOut_KeyCombination_ShouldBeCtrlMinus()
    {
        // Verify zoom out hotkey configuration
        _hotkeyManager.ZoomCameraOut.Key.ShouldBe(GumKey.Subtract);
        _hotkeyManager.ZoomCameraOut.IsCtrlDown.ShouldBeTrue();
    }

    [Fact]
    public void ZoomCameraOutAlternative_KeyCombination_ShouldBeCtrlOemMinus()
    {
        // Verify alternative zoom out hotkey configuration
        _hotkeyManager.ZoomCameraOutAlternative.Key.ShouldBe(GumKey.OemMinus);
        _hotkeyManager.ZoomCameraOutAlternative.IsCtrlDown.ShouldBeTrue();
    }

    [Fact]
    public void PreviewKeyDownAppWide_CtrlF_InvokesFocusSearchAndSetsHandled()
    {
        // Previously untestable: the dispatch used to round-trip through a real WPF KeyEventArgs
        // (Keyboard.PrimaryDevice), which requires an STA thread this test host doesn't guarantee.
        // Now matched directly against the framework-neutral GumKeyEventArgs.
        GumKeyEventArgs e = new() { Key = GumKey.F, IsCtrlDown = true };

        bool handled = _hotkeyManager.PreviewKeyDownAppWide(e);

        handled.ShouldBeTrue();
        e.Handled.ShouldBeTrue();
        _guiCommands.Verify(g => g.FocusSearch(), Times.Once);
    }

    [Fact]
    public void PreviewKeyDownAppWide_CtrlZ_InvokesPerformUndo()
    {
        GumKeyEventArgs e = new() { Key = GumKey.Z, IsCtrlDown = true };

        bool handled = _hotkeyManager.PreviewKeyDownAppWide(e);

        handled.ShouldBeTrue();
        _undoManager.Verify(u => u.PerformUndo(), Times.Once);
    }

    [Fact]
    public void PreviewKeyDownAppWide_CtrlY_InvokesPerformRedo()
    {
        GumKeyEventArgs e = new() { Key = GumKey.Y, IsCtrlDown = true };

        bool handled = _hotkeyManager.PreviewKeyDownAppWide(e);

        handled.ShouldBeTrue();
        _undoManager.Verify(u => u.PerformRedo(), Times.Once);
    }

    [Fact]
    public void PreviewKeyDownAppWide_CtrlShiftZ_InvokesPerformRedo()
    {
        // RedoAlt is Ctrl+Shift+Z.
        GumKeyEventArgs e = new() { Key = GumKey.Z, IsCtrlDown = true, IsShiftDown = true };

        bool handled = _hotkeyManager.PreviewKeyDownAppWide(e);

        handled.ShouldBeTrue();
        _undoManager.Verify(u => u.PerformRedo(), Times.Once);
    }

    [Fact]
    public void PreviewKeyDownAppWide_AltLeft_InvokesNavigateBack()
    {
        GumKeyEventArgs e = new() { Key = GumKey.Left, IsAltDown = true };

        bool handled = _hotkeyManager.PreviewKeyDownAppWide(e);

        handled.ShouldBeTrue();
        _selectionHistory.Verify(s => s.NavigateBack(), Times.Once);
    }

    [Fact]
    public void PreviewKeyDownAppWide_AltRight_InvokesNavigateForward()
    {
        GumKeyEventArgs e = new() { Key = GumKey.Right, IsAltDown = true };

        bool handled = _hotkeyManager.PreviewKeyDownAppWide(e);

        handled.ShouldBeTrue();
        _selectionHistory.Verify(s => s.NavigateForward(), Times.Once);
    }

    [Fact]
    public void PreviewKeyDownAppWide_CtrlAdd_IncreasesBaseFontSize()
    {
        _uiSettingsService.SetupProperty(u => u.BaseFontSize, 12d);
        GumKeyEventArgs e = new() { Key = GumKey.Add, IsCtrlDown = true };

        bool handled = _hotkeyManager.PreviewKeyDownAppWide(e);

        handled.ShouldBeTrue();
        _uiSettingsService.Object.BaseFontSize.ShouldBe(13d);
    }

    [Fact]
    public void PreviewKeyDownAppWide_CtrlAdd_WhenAppWideZoomDisabled_DoesNotHandle()
    {
        _uiSettingsService.SetupProperty(u => u.BaseFontSize, 12d);
        GumKeyEventArgs e = new() { Key = GumKey.Add, IsCtrlDown = true };

        bool handled = _hotkeyManager.PreviewKeyDownAppWide(e, enableEntireAppZoom: false);

        handled.ShouldBeFalse();
        _uiSettingsService.Object.BaseFontSize.ShouldBe(12d);
    }

    [Fact]
    public void PreviewKeyDownAppWide_UnmatchedKey_ReturnsFalseAndLeavesHandledFalse()
    {
        GumKeyEventArgs e = new() { Key = GumKey.C };

        bool handled = _hotkeyManager.PreviewKeyDownAppWide(e);

        handled.ShouldBeFalse();
        e.Handled.ShouldBeFalse();
    }

    [Fact]
    public void HandleKeyDownElementTreeView_Delete_InvokesDeleteSelectionAndSuppressesKeyPress()
    {
        GumKeyEventArgs e = new() { Key = GumKey.Delete };

        _hotkeyManager.HandleKeyDownElementTreeView(e);

        _editCommands.Verify(c => c.DeleteSelection(), Times.Once);
        e.Handled.ShouldBeTrue();
        e.SuppressKeyPress.ShouldBeTrue();
    }

    [Fact]
    public void HandleKeyDownElementTreeView_CtrlZ_DispatchesToAppWideUndo_NotDelete()
    {
        // App-wide keys (Undo/Redo/Search/...) take precedence over the element-tree-specific keys.
        GumKeyEventArgs e = new() { Key = GumKey.Z, IsCtrlDown = true };

        _hotkeyManager.HandleKeyDownElementTreeView(e);

        _undoManager.Verify(u => u.PerformUndo(), Times.Once);
        _editCommands.Verify(c => c.DeleteSelection(), Times.Never);
    }

    [Fact]
    public void HandleKeyDownElementTreeView_CtrlC_InvokesOnCopyAndSuppressesKeyPress()
    {
        GumKeyEventArgs e = new() { Key = GumKey.C, IsCtrlDown = true };

        _hotkeyManager.HandleKeyDownElementTreeView(e);

        _copyPasteLogic.Verify(c => c.OnCopy(CopyType.InstanceOrElement), Times.Once);
        e.Handled.ShouldBeTrue();
        e.SuppressKeyPress.ShouldBeTrue();
    }

    [Fact]
    public void HandleEditorKeyDown_Delete_InvokesDeleteSelection()
    {
        GumKeyEventArgs e = new() { Key = GumKey.Delete };

        _hotkeyManager.HandleEditorKeyDown(e);

        _editCommands.Verify(c => c.DeleteSelection(), Times.Once);
        e.Handled.ShouldBeTrue();
    }

    [Fact]
    public void HandleEditorKeyDown_CtrlAdd_DoesNotDispatchAppWideZoom()
    {
        // HandleEditorKeyDown calls the app-wide dispatch with enableEntireAppZoom: false -
        // zoom in the wireframe editor is handled elsewhere (e.g. CameraController).
        _uiSettingsService.SetupProperty(u => u.BaseFontSize, 12d);
        GumKeyEventArgs e = new() { Key = GumKey.Add, IsCtrlDown = true };

        _hotkeyManager.HandleEditorKeyDown(e);

        _uiSettingsService.Object.BaseFontSize.ShouldBe(12d);
    }

    private (ComponentSave element, InstanceSave instance) SetUpSelectedInstance(float x, float y, bool locked = false)
    {
        ComponentSave element = new ComponentSave();
        element.States.Add(new StateSave());
        element.DefaultState.ParentContainer = element;

        InstanceSave instance = new InstanceSave { Name = "MyInstance", BaseType = "Container" };
        instance.Locked = locked;
        element.Instances.Add(instance);

        element.DefaultState.SetValue("MyInstance.X", x);
        element.DefaultState.SetValue("MyInstance.Y", y);

        _selectedState.Setup(s => s.SelectedInstance).Returns(instance);
        _selectedState.Setup(s => s.SelectedElement).Returns(element);

        return (element, instance);
    }
}
