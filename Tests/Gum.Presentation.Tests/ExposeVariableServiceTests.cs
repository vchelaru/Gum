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
using System;
using System.Collections.Generic;
using System.Linq;

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
            _pluginManager.Object,
            new InstanceOverrideRemover(_undoManager.Object, _fileCommands.Object, _pluginManager.Object));
    }

    private delegate void ShowChoiceCallback(Action<ChoiceDialogViewModel>? initializer, out ChoiceDialogViewModel viewModel);

    /// <summary>
    /// Answers the next choice dialog: <paramref name="pick"/> is the option text the user selects, or
    /// null for Cancel. Returns the shown dialog through <paramref name="shown"/>.
    /// </summary>
    private void AnswerChoiceDialog(string? pick, List<ChoiceDialogViewModel> shown)
    {
        _dialogService
            .Setup(x => x.Show(It.IsAny<Action<ChoiceDialogViewModel>?>(), out It.Ref<ChoiceDialogViewModel>.IsAny))
            .Callback(new ShowChoiceCallback((Action<ChoiceDialogViewModel>? initializer, out ChoiceDialogViewModel viewModel) =>
            {
                viewModel = new ChoiceDialogViewModel();
                initializer?.Invoke(viewModel);
                shown.Add(viewModel);
                if (pick == null)
                {
                    viewModel.NegativeCommand.Execute(null);
                }
                else
                {
                    viewModel.SelectedValue = pick;
                    viewModel.AffirmativeCommand.Execute(null);
                }
            }))
            .Returns(pick != null);
    }

    /// <summary>
    /// Button exposes Label.Text as LabelText; screen Title's OkButton sets LabelText.
    /// </summary>
    private (ComponentSave Button, VariableSave Exposed, ScreenSave Title, VariableSave InstanceValue) MakeExposedVariableSetOnAnInstance()
    {
        ComponentSave button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        VariableSave exposed = new VariableSave { Name = "Label.Text", Type = "string", ExposedAsName = "LabelText" };
        button.GetDefaultStateOrThrow().Variables.Add(exposed);

        ScreenSave title = new ScreenSave { Name = "Title" };
        title.States.Add(new StateSave { Name = "Default", ParentContainer = title });
        title.Instances.Add(new InstanceSave { Name = "OkButton", BaseType = "Button", ParentContainer = title });
        VariableSave instanceValue = new VariableSave { Name = "OkButton.LabelText", Type = "string", Value = "OK" };
        title.GetDefaultStateOrThrow().Variables.Add(instanceValue);

        VariableChangeResponse changes = new VariableChangeResponse();
        changes.VariableChanges.Add(new VariableChange { Container = title, State = title.GetDefaultStateOrThrow(), Variable = instanceValue });
        _renameLogic
            .Setup(x => x.GetChangesForRenamedVariable(button, "Label.Text", "LabelText"))
            .Returns(changes);

        return (button, exposed, title, instanceValue);
    }

    [Fact]
    public void ExposeVariable_ShouldSetExposedAsNameAndNotifyPlugins_WhenVariableAlreadyExists()
    {
        var component = new ComponentSave { Name = "MyComponent" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        var instance = new InstanceSave { Name = "MyInstance", ParentContainer = component };
        component.Instances.Add(instance);

        var variable = new VariableSave { Name = "MyInstance.Visible", Value = true };
        component.GetDefaultStateOrThrow().Variables.Add(variable);

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
    public void HandleExposeVariableClick_OnABehaviorsInstance_ShowsAMessageInsteadOfThrowing()
    {
        // A behavior's instance has no element to expose the variable on, and while the behavior
        // is selected there is no selected element either.
        var instance = new InstanceSave { Name = "TextInstance", BaseType = "Text" };
        _selectedState.Setup(x => x.SelectedElement).Returns((ElementSave?)null);

        var response = _service.HandleExposeVariableClick(instance, "Text");

        response.Succeeded.ShouldBeTrue();
        response.DidAttempt.ShouldBeFalse();
        _dialogService.Verify(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle?>()), Times.Once);
        _pluginManager.Verify(x => x.VariableAdd(It.IsAny<ElementSave?>(), It.IsAny<string>()), Times.Never);
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
        _dialogService.Verify(x => x.Show(It.IsAny<Action<ChoiceDialogViewModel>?>(), out It.Ref<ChoiceDialogViewModel>.IsAny), Times.Never);
    }

    [Fact]
    public void HandleUnexposeVariableClick_WhenOnlyAnInheritingElementExposesTheSameName_ShowsNoDialog()
    {
        // FancyButton exposes a different variable of its own under the same name. That is not a value
        // for Button's variable, so there is nothing to clear.
        ComponentSave button = new ComponentSave { Name = "Button" };
        VariableSave exposed = new VariableSave { Name = "Label.Text", ExposedAsName = "LabelText" };
        ComponentSave fancyButton = new ComponentSave { Name = "FancyButton", BaseType = "Button" };
        StateSave fancyDefault = new StateSave { Name = "Default", ParentContainer = fancyButton };
        fancyButton.States.Add(fancyDefault);
        VariableSave derivedExposed = new VariableSave { Name = "FancyLabel.Text", ExposedAsName = "LabelText" };
        fancyDefault.Variables.Add(derivedExposed);
        VariableChangeResponse changes = new VariableChangeResponse();
        changes.VariableChanges.Add(new VariableChange { Container = fancyButton, State = fancyDefault, Variable = derivedExposed });
        _renameLogic
            .Setup(x => x.GetChangesForRenamedVariable(button, "Label.Text", "LabelText"))
            .Returns(changes);

        _service.HandleUnexposeVariableClick(exposed, button);

        exposed.ExposedAsName.ShouldBeNull();
        fancyDefault.Variables.ShouldContain(derivedExposed);
        _dialogService.Verify(x => x.Show(It.IsAny<Action<ChoiceDialogViewModel>?>(), out It.Ref<ChoiceDialogViewModel>.IsAny), Times.Never);
    }

    [Fact]
    public void HandleUnexposeVariableClick_ClearChosen_WhenAnInheritingElementSetsTheValue_ListsAndRemovesIt()
    {
        ComponentSave button = new ComponentSave { Name = "Button" };
        VariableSave exposed = new VariableSave { Name = "Label.Text", ExposedAsName = "LabelText" };
        ComponentSave fancyButton = new ComponentSave { Name = "FancyButton", BaseType = "Button" };
        StateSave fancyDefault = new StateSave { Name = "Default", ParentContainer = fancyButton };
        fancyButton.States.Add(fancyDefault);
        VariableSave derivedValue = new VariableSave { Name = "Label.Text", Type = "string", Value = "Fancy", ExposedAsName = "LabelText" };
        fancyDefault.Variables.Add(derivedValue);
        VariableChangeResponse changes = new VariableChangeResponse();
        changes.VariableChanges.Add(new VariableChange { Container = fancyButton, State = fancyDefault, Variable = derivedValue, IsInheritingElementValue = true });
        _renameLogic
            .Setup(x => x.GetChangesForRenamedVariable(button, "Label.Text", "LabelText"))
            .Returns(changes);
        List<ChoiceDialogViewModel> shown = new List<ChoiceDialogViewModel>();
        AnswerChoiceDialog("Un-expose and clear values", shown);

        _service.HandleUnexposeVariableClick(exposed, button);

        shown.Single().Message.ShouldContain("FancyButton (derived)");
        exposed.ExposedAsName.ShouldBeNull();
        fancyDefault.Variables.ShouldNotContain(derivedValue);
        _pluginManager.Verify(x => x.VariableSet(fancyButton, null, "LabelText", null, It.IsAny<bool>()), Times.Once);
        _undoManager.Verify(x => x.RecordCrossElementVariableChanges(It.IsAny<IEnumerable<CrossElementVariableChange>>()), Times.Once);
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

    [Fact]
    public void HandleUnexposeVariableClick_ClearChosen_UnexposesAndRemovesInstanceValuesInOneUndo()
    {
        (ComponentSave button, VariableSave exposed, ScreenSave title, VariableSave instanceValue) = MakeExposedVariableSetOnAnInstance();
        List<ChoiceDialogViewModel> shown = new List<ChoiceDialogViewModel>();
        AnswerChoiceDialog("Un-expose and clear values", shown);
        bool isLockHeld = false;
        bool? wasLockHeldWhenRecorded = null;
        _undoManager.Setup(x => x.RequestLock()).Returns(() =>
        {
            isLockHeld = true;
            return new UndoLock(() => isLockHeld = false);
        });
        _undoManager
            .Setup(x => x.RecordCrossElementVariableChanges(It.IsAny<IEnumerable<CrossElementVariableChange>>()))
            .Callback(() => wasLockHeldWhenRecorded = isLockHeld);

        _service.HandleUnexposeVariableClick(exposed, button);

        shown.Single().Message.ShouldContain("OkButton in Title");
        shown.Single().OptionValues.ShouldBe(new[] { "Un-expose and clear values", "Un-expose, keep values" });
        shown.Single().CanCancel.ShouldBeTrue();
        exposed.ExposedAsName.ShouldBeNull();
        title.GetDefaultStateOrThrow().Variables.ShouldNotContain(instanceValue);
        wasLockHeldWhenRecorded.ShouldBe(true);
        _undoManager.Verify(x => x.RequestLock(), Times.Once);
        _pluginManager.Verify(x => x.VariableDelete(button, "LabelText"), Times.Once);
    }

    [Fact]
    public void HandleUnexposeVariableClick_KeepChosen_UnexposesAndLeavesInstanceValues()
    {
        (ComponentSave button, VariableSave exposed, ScreenSave title, VariableSave instanceValue) = MakeExposedVariableSetOnAnInstance();
        AnswerChoiceDialog("Un-expose, keep values", new List<ChoiceDialogViewModel>());

        _service.HandleUnexposeVariableClick(exposed, button);

        exposed.ExposedAsName.ShouldBeNull();
        title.GetDefaultStateOrThrow().Variables.ShouldContain(instanceValue);
        _undoManager.Verify(x => x.RecordCrossElementVariableChanges(It.IsAny<IEnumerable<CrossElementVariableChange>>()), Times.Never);
        _pluginManager.Verify(x => x.VariableDelete(button, "LabelText"), Times.Once);
    }

    [Fact]
    public void HandleUnexposeVariableClick_Cancelled_ChangesNothing()
    {
        (ComponentSave button, VariableSave exposed, ScreenSave title, VariableSave instanceValue) = MakeExposedVariableSetOnAnInstance();
        AnswerChoiceDialog(null, new List<ChoiceDialogViewModel>());

        _service.HandleUnexposeVariableClick(exposed, button);

        exposed.ExposedAsName.ShouldBe("LabelText");
        title.GetDefaultStateOrThrow().Variables.ShouldContain(instanceValue);
        _undoManager.Verify(x => x.RequestLock(), Times.Never);
        _pluginManager.Verify(x => x.VariableDelete(It.IsAny<ElementSave>(), It.IsAny<string>()), Times.Never);
    }
}
