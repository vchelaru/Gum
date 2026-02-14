using Gum.Commands;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
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
    private readonly Mock<IDeleteLogic> _deleteLogic;
    private readonly Mock<IReorderLogic> _reorderLogic;
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
        _deleteLogic = new Mock<IDeleteLogic>();
        _reorderLogic = new Mock<IReorderLogic>();

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
            _deleteLogic.Object,
            _reorderLogic.Object
        );
    }

    [Fact]
    public void ZoomCameraIn_KeyCombination_ShouldBeCtrlPlus()
    {
        // Verify zoom hotkey configuration
        _hotkeyManager.ZoomCameraIn.Key.ShouldBe(System.Windows.Forms.Keys.Add);
        _hotkeyManager.ZoomCameraIn.IsCtrlDown.ShouldBeTrue();
    }

    [Fact]
    public void ZoomCameraInAlternative_KeyCombination_ShouldBeCtrlOemPlus()
    {
        // Verify alternative zoom hotkey configuration
        _hotkeyManager.ZoomCameraInAlternative.Key.ShouldBe(System.Windows.Forms.Keys.Oemplus);
        _hotkeyManager.ZoomCameraInAlternative.IsCtrlDown.ShouldBeTrue();
    }

    [Fact]
    public void ZoomCameraOut_KeyCombination_ShouldBeCtrlMinus()
    {
        // Verify zoom out hotkey configuration
        _hotkeyManager.ZoomCameraOut.Key.ShouldBe(System.Windows.Forms.Keys.Subtract);
        _hotkeyManager.ZoomCameraOut.IsCtrlDown.ShouldBeTrue();
    }

    [Fact]
    public void ZoomCameraOutAlternative_KeyCombination_ShouldBeCtrlOemMinus()
    {
        // Verify alternative zoom out hotkey configuration
        _hotkeyManager.ZoomCameraOutAlternative.Key.ShouldBe(System.Windows.Forms.Keys.OemMinus);
        _hotkeyManager.ZoomCameraOutAlternative.IsCtrlDown.ShouldBeTrue();
    }
}
