using Gum.Commands;
using Gum.DataTypes;
using Gum.Input;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.Managers;

/// <summary>The platform-dependent defaults of <see cref="HotkeyManager"/>.</summary>
public class HotkeyManagerTests
{
    [Fact]
    public void ShowHotkeys_KeyCombination_ShouldBeCtrlSlash()
    {
        AutoMocker mocker = new AutoMocker();
        HotkeyManager hotkeyManager = mocker.CreateInstance<HotkeyManager>();

        hotkeyManager.ShowHotkeys.Key.ShouldBe(GumKey.OemQuestion);
        hotkeyManager.ShowHotkeys.IsCtrlDown.ShouldBeTrue();
    }

    [Fact]
    public void PreviewKeyDownAppWide_CtrlSlash_InvokesGuiCommandsShowHotkeysAndSetsHandled()
    {
        AutoMocker mocker = new AutoMocker();
        HotkeyManager hotkeyManager = mocker.CreateInstance<HotkeyManager>();
        GumKeyEventArgs e = new() { Key = GumKey.OemQuestion, IsCtrlDown = true };

        bool handled = hotkeyManager.PreviewKeyDownAppWide(e);

        handled.ShouldBeTrue();
        e.Handled.ShouldBeTrue();
        mocker.GetMock<IGuiCommands>().Verify(g => g.ShowHotkeys(), Times.Once);
    }

    [Theory]
    [InlineData(false, GumKey.Y, false, GumKey.Z, true)]
    [InlineData(true, GumKey.Z, true, GumKey.Y, false)]
    public void Redo_IsCtrlY_ExceptOnMacOS_WhereItIsShiftCmdZ(bool isMacOS,
        GumKey redoKey, bool redoShift, GumKey redoAltKey, bool redoAltShift)
    {
        AutoMocker mocker = new AutoMocker();
        mocker.GetMock<IOperatingSystemInfo>().Setup(o => o.IsMacOS).Returns(isMacOS);

        HotkeyManager hotkeyManager = mocker.CreateInstance<HotkeyManager>();

        hotkeyManager.Redo.Key.ShouldBe(redoKey);
        hotkeyManager.Redo.IsCtrlDown.ShouldBeTrue();
        hotkeyManager.Redo.IsShiftDown.ShouldBe(redoShift);
        hotkeyManager.RedoAlt.Key.ShouldBe(redoAltKey);
        hotkeyManager.RedoAlt.IsCtrlDown.ShouldBeTrue();
        hotkeyManager.RedoAlt.IsShiftDown.ShouldBe(redoAltShift);
    }

    [Fact]
    public void HandleKeyDownElementTreeView_F2OnInstance_ValidatesNameAheadOfTime()
    {
        // Rename Instance must validate as the user types (like Rename Behavior/Rename Element
        // already do), instead of only finding out after OK has already closed the dialog -- see
        // https://github.com/vchelaru/Gum/issues/4888
        AutoMocker mocker = new AutoMocker();

        var container = new ComponentSave();
        var instance = new InstanceSave { Name = "OldName", ParentContainer = container };
        mocker.GetMock<ISelectedState>().Setup(s => s.SelectedInstance).Returns(instance);

        string whyNotValid = "bad name";
        mocker.GetMock<INameVerifier>()
            .Setup(v => v.IsInstanceNameValid("Invalid@Name", instance, container, out whyNotValid))
            .Returns(false);
        string noError = null;
        mocker.GetMock<INameVerifier>()
            .Setup(v => v.IsInstanceNameValid("ValidName", instance, container, out noError))
            .Returns(true);

        GetUserStringOptions capturedOptions = null;
        mocker.GetMock<IDialogService>()
            .Setup(d => d.GetUserString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetUserStringOptions>()))
            .Callback<string, string, GetUserStringOptions>((_, _, options) => capturedOptions = options)
            .Returns((string)null);

        HotkeyManager hotkeyManager = mocker.CreateInstance<HotkeyManager>();
        hotkeyManager.HandleKeyDownElementTreeView(new GumKeyEventArgs { Key = GumKey.F2 });

        capturedOptions.ShouldNotBeNull();
        capturedOptions.Validator.ShouldNotBeNull();
        capturedOptions.Validator!("Invalid@Name").ShouldBe("bad name");
        capturedOptions.Validator!("ValidName").ShouldBeNull();
    }
}
