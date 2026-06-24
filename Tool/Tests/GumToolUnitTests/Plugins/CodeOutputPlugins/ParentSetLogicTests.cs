using CodeOutputPlugin.Manager;
using Gum.Commands;
using Gum.DataTypes.Variables;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Xunit;

namespace GumToolUnitTests.Plugins.CodeOutputPlugins;

public class ParentSetLogicTests : BaseTestClass
{
    // ParentSetLogic now takes ISelectedState + IDialogService + IFileCommands via its constructor
    // (drained from field-initializer Locator.GetRequiredService calls). These pin that the early-out
    // paths of HandleVariableSet never touch the injected dialog/file dependencies. Because those
    // guard paths never dereference the CodeGenerator either, passing null for it here is safe (and
    // avoids standing up its multi-argument graph just to verify a guard).
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly ParentSetLogic _parentSetLogic;

    public ParentSetLogicTests()
    {
        _parentSetLogic = new ParentSetLogic(
            codeGenerator: null!,
            _selectedState.Object,
            _dialogService.Object,
            _fileCommands.Object);
    }

    [Fact]
    public void HandleVariableSet_does_nothing_when_no_state_is_selected()
    {
        _selectedState.Setup(x => x.SelectedStateSave).Returns((StateSave?)null);

        _parentSetLogic.HandleVariableSet(
            element: null!,
            instance: null,
            variableName: "Parent",
            oldValue: null,
            codeOutputProjectSettings: null!);

        _dialogService.VerifyNoOtherCalls();
        _fileCommands.VerifyNoOtherCalls();
    }

    [Fact]
    public void HandleVariableSet_does_nothing_for_non_parent_variable()
    {
        _selectedState.Setup(x => x.SelectedStateSave).Returns(new StateSave());

        _parentSetLogic.HandleVariableSet(
            element: null!,
            instance: null,
            variableName: "X",
            oldValue: null,
            codeOutputProjectSettings: null!);

        _dialogService.VerifyNoOtherCalls();
        _fileCommands.VerifyNoOtherCalls();
    }
}
