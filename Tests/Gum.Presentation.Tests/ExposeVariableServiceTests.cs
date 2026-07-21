using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;
using System.Collections.Generic;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pinning tests for <see cref="ExposeVariableService"/> — added when the class moved from
/// <c>Gum.csproj</c> into the headless <c>Gum.Presentation</c> assembly (ADR-0005 Phase 3, #3909).
/// The move was behavior-preserving, so these characterize existing behavior rather than TDD-driving
/// new behavior.
/// </summary>
public class ExposeVariableServiceTests : BaseTestClass
{
    private readonly Mock<IUndoManager> _undoManager;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IRenameLogic> _renameLogic;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IVariableSaveLogic> _variableSaveLogic;
    private readonly Mock<IPluginManager> _pluginManager;
    private readonly ExposeVariableService _service;

    public ExposeVariableServiceTests()
    {
        _undoManager = new Mock<IUndoManager>();
        _undoManager.Setup(x => x.RequestLock()).Returns(new UndoLock(() => { }));
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _renameLogic = new Mock<IRenameLogic>();
        _selectedState = new Mock<ISelectedState>();
        _nameVerifier = new Mock<INameVerifier>();
        _dialogService = new Mock<IDialogService>();
        _variableSaveLogic = new Mock<IVariableSaveLogic>();
        _pluginManager = new Mock<IPluginManager>();

        _service = new ExposeVariableService(
            _undoManager.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            _renameLogic.Object,
            _selectedState.Object,
            _nameVerifier.Object,
            _dialogService.Object,
            _variableSaveLogic.Object,
            _pluginManager.Object);
    }

    [Fact]
    public void ExposeVariable_ShouldSetExposedAsNameAndNotifyPlugins_WhenVariableAlreadyExists()
    {
        var component = new ComponentSave { Name = "MyComponent" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        var instance = new InstanceSave { Name = "MyInstance", ParentContainer = component };
        component.Instances.Add(instance);

        var variable = new VariableSave { Name = "MyInstance.Visible", Value = true };
        component.DefaultState.Variables.Add(variable);

        _selectedState.Setup(x => x.SelectedElement).Returns(component);
        _selectedState.Setup(x => x.SelectedStateSave).Returns((StateSave?)null);

        _variableSaveLogic
            .Setup(x => x.GetIfVariableIsActive(It.IsAny<VariableSave>(), It.IsAny<ElementSave>(), It.IsAny<InstanceSave?>()))
            .Returns(true);

        var response = _service.ExposeVariable(instance, "Visible", "MyExposedVisible");

        response.Succeeded.ShouldBeTrue();
        response.Data!.ExposedAsName.ShouldBe("MyExposedVisible");
        _pluginManager.Verify(x => x.VariableAdd(component, "MyExposedVisible"), Times.Once);
        _fileCommands.Verify(x => x.TryAutoSaveCurrentElement(), Times.Once);
        _guiCommands.Verify(x => x.RefreshVariables(true), Times.Once);
    }

    [Fact]
    public void HandleUnexposeVariableClick_ShouldClearExposedAsNameAndNotifyPlugins_WhenNoReferencesExist()
    {
        var component = new ComponentSave { Name = "MyComponent" };
        var variable = new VariableSave { Name = "MyInstance.Visible", ExposedAsName = "MyExposedVisible" };

        _renameLogic
            .Setup(x => x.GetChangesForRenamedVariable(component, variable.Name, "MyExposedVisible"))
            .Returns(new VariableChangeResponse());

        _service.HandleUnexposeVariableClick(variable, component);

        variable.ExposedAsName.ShouldBeNull();
        _pluginManager.Verify(x => x.VariableDelete(component, "MyExposedVisible"), Times.Once);
        _fileCommands.Verify(x => x.TryAutoSaveCurrentElement(), Times.Once);
        _guiCommands.Verify(x => x.RefreshVariables(true), Times.Once);
    }

    [Fact]
    public void HandleUnexposeVariableClick_ShouldShowMessageAndNotClear_WhenVariableIsReferenced()
    {
        var component = new ComponentSave { Name = "MyComponent" };
        var variable = new VariableSave { Name = "MyInstance.Visible", ExposedAsName = "MyExposedVisible" };

        var changes = new VariableChangeResponse();
        changes.VariableReferenceChanges.Add(new VariableReferenceChange
        {
            Container = component,
            LineIndex = 0,
            VariableReferenceList = new VariableListSave<string> { Name = "SomeVariableReferences", Value = new List<string> { "SomeInstance.Visible = true" } }
        });

        _renameLogic
            .Setup(x => x.GetChangesForRenamedVariable(component, variable.Name, "MyExposedVisible"))
            .Returns(changes);

        _service.HandleUnexposeVariableClick(variable, component);

        variable.ExposedAsName.ShouldBe("MyExposedVisible");
        _dialogService.Verify(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()), Times.Once);
        _pluginManager.Verify(x => x.VariableDelete(It.IsAny<ElementSave>(), It.IsAny<string>()), Times.Never);
    }
}
