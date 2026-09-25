using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Responses;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

public class EditCommandsTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly EditCommands _editCommands;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly Mock<IReferenceFinder> _referenceFinder;
    private readonly GumProjectSave _gumProject;

    public EditCommandsTests()
    {
        _mocker = new AutoMocker();

        _dialogService = _mocker.GetMock<IDialogService>();
        _fileCommands = _mocker.GetMock<IFileCommands>();
        _projectManager = _mocker.GetMock<IProjectManager>();
        _referenceFinder = _mocker.GetMock<IReferenceFinder>();

        _gumProject = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _gumProject;
        _projectManager.Setup(m => m.GumProjectSave).Returns(_gumProject);

        // Default: GetFullPathXmlFile returns a non-existent path so no file deletion occurs
        _fileCommands
            .Setup(f => f.GetFullPathXmlFile(It.IsAny<BehaviorSave>()))
            .Returns(new FilePath("C:/nonexistent/test.behx"));

        // Default: no element references to the behavior
        _referenceFinder
            .Setup(r => r.GetReferencesToBehavior(It.IsAny<BehaviorSave>(), It.IsAny<string>()))
            .Returns(new BehaviorReferences());

        _editCommands = _mocker.CreateInstance<EditCommands>();
    }

    [Fact]
    public void AskToRenameBehavior_UpdatesElementBehaviorReferences()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        _gumProject.Behaviors.Add(behavior);
        _gumProject.BehaviorReferences.Add(new BehaviorReference { Name = "MyBehavior" });

        var component = new ComponentSave { Name = "MyButton" };
        var behaviorRef = new ElementBehaviorReference { BehaviorName = "MyBehavior" };
        component.Behaviors.Add(behaviorRef);
        _gumProject.Components.Add(component);

        var refs = new BehaviorReferences();
        refs.ElementsWithBehaviorReference.Add((component, behaviorRef));
        _referenceFinder
            .Setup(r => r.GetReferencesToBehavior(behavior, "MyBehavior"))
            .Returns(refs);

        _dialogService
            .Setup(d => d.GetUserString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetUserStringOptions>()))
            .Returns("MyBehaviorRenamed");

        _editCommands.AskToRenameBehavior(behavior);

        behaviorRef.BehaviorName.ShouldBe("MyBehaviorRenamed");
    }

    [Fact]
    public void AskToDeleteState_ResetsInstanceStateReference_WhenStateNameIsNotSameStringInstance()
    {
        // A state name loaded from disk is a different string instance from StateSave.Name,
        // so the reference check must compare by value -- see https://github.com/vchelaru/Gum/issues/5003
        var component = new ComponentSave { Name = "MyButton" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = component };
        var highlighted = new StateSave { Name = "Highlighted", ParentContainer = component };
        component.States.Add(defaultState);
        component.States.Add(highlighted);
        _gumProject.Components.Add(component);

        var screen = new ScreenSave { Name = "MainScreen" };
        screen.Instances.Add(new InstanceSave { Name = "ButtonInstance", BaseType = "MyButton", ParentContainer = screen });
        var screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        var stateVariable = new VariableSave
        {
            Name = "ButtonInstance.State",
            Type = "State",
            Value = new string("Highlighted".ToCharArray())
        };
        screenDefault.Variables.Add(stateVariable);
        screen.States.Add(screenDefault);
        _gumProject.Screens.Add(screen);

        _mocker.GetMock<ISelectedState>().Setup(s => s.SelectedElement).Returns(component);
        _mocker.GetMock<IPluginManager>()
            .Setup(p => p.GetDeleteStateResponse(highlighted, component))
            .Returns(new DeleteResponse { ShouldDelete = true });
        _referenceFinder
            .Setup(r => r.GetReferencesToState(highlighted, "Highlighted", component, null))
            .Returns(new StateReferences());
        _dialogService
            .Setup(d => d.ShowMessage(It.IsAny<string>(), "Delete state?", It.IsAny<MessageDialogStyle?>()))
            .Returns(MessageDialogResult.Affirmative);
        var choice = new ChoiceDialogViewModel();
        choice.SetOptions(new Dictionary<string, string> { ["make-default"] = "Change variable to default" });
        string? shownMessage = null;
        _dialogService
            .Setup(d => d.Show(It.IsAny<Action<ChoiceDialogViewModel>>(), out choice))
            .Callback(new ShowChoiceCallback((Action<ChoiceDialogViewModel>? initializer, out ChoiceDialogViewModel viewModel) =>
            {
                var probe = new ChoiceDialogViewModel();
                initializer?.Invoke(probe);
                shownMessage = probe.Message;
                viewModel = choice;
            }))
            .Returns(true);

        _editCommands.AskToDeleteState(highlighted, component);

        stateVariable.Value.ShouldBe("Default");
        shownMessage.ShouldNotBeNull();
        shownMessage.ShouldContain("used in the element MainScreen");
    }

    private delegate void ShowChoiceCallback(Action<ChoiceDialogViewModel>? initializer, out ChoiceDialogViewModel viewModel);

    [Fact]
    public void AskToRenameBehavior_UpdatesProjectBehaviorReference()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        _gumProject.Behaviors.Add(behavior);
        var projectRef = new BehaviorReference { Name = "MyBehavior" };
        _gumProject.BehaviorReferences.Add(projectRef);

        _dialogService
            .Setup(d => d.GetUserString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetUserStringOptions>()))
            .Returns("MyBehaviorRenamed");

        _editCommands.AskToRenameBehavior(behavior);

        projectRef.Name.ShouldBe("MyBehaviorRenamed");
        behavior.Name.ShouldBe("MyBehaviorRenamed");
    }

    [Fact]
    public void AskToRenameState_PassesValidatorThatUsesNameVerifier()
    {
        // Rename state must validate as the user types, like Rename Behavior/Rename Instance --
        // see https://github.com/vchelaru/Gum/issues/4889
        var component = new ComponentSave { Name = "MyComponent" };
        var stateSave = new StateSave { Name = "OldState", ParentContainer = component };
        component.States.Add(stateSave);

        _mocker.GetMock<ISelectedState>().Setup(s => s.SelectedStateSave).Returns(stateSave);
        _mocker.GetMock<IRenameLogic>()
            .Setup(r => r.GetChangesForRenamedState(stateSave, stateSave.Name, component, null))
            .Returns(new StateReferences());

        GetUserStringOptions capturedOptions = null;
        _dialogService
            .Setup(d => d.GetUserString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetUserStringOptions>()))
            .Callback<string, string, GetUserStringOptions>((_, _, options) => capturedOptions = options)
            .Returns((string)null);

        string whyNotValid = "bad name";
        _mocker.GetMock<INameVerifier>()
            .Setup(v => v.IsStateNameValid("Invalid@Name", null, stateSave, out whyNotValid))
            .Returns(false);

        _editCommands.AskToRenameState(stateSave, component);

        capturedOptions.ShouldNotBeNull();
        capturedOptions.Validator.ShouldNotBeNull();
        capturedOptions.Validator!("Invalid@Name").ShouldBe("bad name");
    }
}
