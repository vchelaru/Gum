using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.VariableGrid;
using Gum.Services;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for MainControlViewModel, relocated out of Gum.csproj into the
/// headless Gum.Presentation assembly (ADR-0005, #3754). Its behavior-variable right-click menu was
/// rebuilt on the ContextMenuItemViewModel pattern from #3786 instead of constructing WPF MenuItems
/// directly in the constructor.
/// </summary>
public class VariableGridMainControlViewModelTests
{
    private readonly Mock<IDeleteVariableService> _deleteVariableService;
    private readonly Mock<IEditVariableService> _editVariableService;
    private readonly MainControlViewModel _sut;

    public VariableGridMainControlViewModelTests()
    {
        _deleteVariableService = new Mock<IDeleteVariableService>();
        _editVariableService = new Mock<IEditVariableService>();

        _sut = new MainControlViewModel(_deleteVariableService.Object, _editVariableService.Object);
    }

    [Fact]
    public void BehaviorVariablesContextMenuItems_DeleteAction_ShouldDeleteVariable()
    {
        BehaviorSave behaviorSave = new BehaviorSave();
        VariableSave variable = new VariableSave { Name = "TestVariable" };
        _sut.BehaviorSave = behaviorSave;
        _sut.SelectedBehaviorVariable = variable;

        _sut.BehaviorVariablesContextMenuItems[1].Action!();

        _deleteVariableService.Verify(x => x.DeleteVariable(variable, behaviorSave), Times.Once);
    }

    [Fact]
    public void BehaviorVariablesContextMenuItems_EditAction_ShouldShowEditVariableWindow_WhenEditModeAvailable()
    {
        BehaviorSave behaviorSave = new BehaviorSave();
        VariableSave variable = new VariableSave { Name = "TestVariable" };
        _sut.BehaviorSave = behaviorSave;
        _sut.SelectedBehaviorVariable = variable;
        _editVariableService
            .Setup(x => x.GetAvailableEditModeFor(variable, behaviorSave))
            .Returns(VariableEditMode.FullEdit);

        _sut.BehaviorVariablesContextMenuItems[0].Text.ShouldBe("Edit Variable");
        _sut.BehaviorVariablesContextMenuItems[0].Action!();

        _editVariableService.Verify(x => x.ShowEditVariableWindow(variable, behaviorSave), Times.Once);
    }

    [Fact]
    public void BehaviorVariablesContextMenuItems_ShouldBeEmpty_WhenNoVariableSelected()
    {
        _sut.SelectedBehaviorVariable = null!;

        _sut.BehaviorVariablesContextMenuItems.ShouldBeEmpty();
    }

    [Fact]
    public void EffectiveSelectedBehaviorVariable_ShouldOnlyReturnVariable_WhenBehaviorUiShown()
    {
        VariableSave variable = new VariableSave { Name = "TestVariable" };
        _sut.SelectedBehaviorVariable = variable;

        _sut.ShowBehaviorUi = false;
        _sut.EffectiveSelectedBehaviorVariable.ShouldBeNull();

        _sut.ShowBehaviorUi = true;
        _sut.EffectiveSelectedBehaviorVariable.ShouldBe(variable);
    }
}
