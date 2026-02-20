using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Commands;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;

namespace GumToolUnitTests.ViewModels;

public class AddVariableViewModelTests : BaseTestClass
{
    private readonly Mock<IUndoManager> _undoManager;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IElementCommands> _elementCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly Mock<IDialogService> _dialogService;
    private readonly AddVariableViewModel _viewModel;
    private readonly ComponentSave _component;

    public AddVariableViewModelTests()
    {
        _component = new ComponentSave();
        _component.Name = "TestComponent";
        _component.States.Add(new StateSave { Name = "Default", ParentContainer = _component });

        _undoManager = new Mock<IUndoManager>();
        _selectedState = new Mock<ISelectedState>();
        _guiCommands = new Mock<IGuiCommands>();
        _elementCommands = new Mock<IElementCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _nameVerifier = new Mock<INameVerifier>();
        _dialogService = new Mock<IDialogService>();

        _selectedState.Setup(x => x.SelectedElement).Returns(_component);
        _selectedState.Setup(x => x.SelectedBehavior).Returns((BehaviorSave?)null);

        string? whyNotValid = null;
        _nameVerifier
            .Setup(x => x.IsVariableNameValid(It.IsAny<string>(), It.IsAny<ElementSave>(), It.IsAny<VariableSave>(), out whyNotValid))
            .Returns(true);

        _viewModel = new AddVariableViewModel(
            _guiCommands.Object,
            _undoManager.Object,
            _elementCommands.Object,
            _fileCommands.Object,
            _nameVerifier.Object,
            _selectedState.Object,
            _dialogService.Object);

        var gumProject = new GumProjectSave();
        gumProject.Components.Add(_component);
        ObjectFinder.Self.GumProjectSave = gumProject;
    }

    [Fact]
    public void RenameCustomVariable_ShouldRequestUndoLock()
    {
        var customVariable = new VariableSave
        {
            Name = "OldVar",
            Type = "float",
            IsCustomVariable = true,
            Value = 0f
        };
        _component.DefaultState.Variables.Add(customVariable);

        _viewModel.Variable = customVariable;
        _viewModel.Element = _component;
        _viewModel.EnteredName = "NewVar";
        _viewModel.SelectedItem = "float";
        _viewModel.VariableChangeResponse = new VariableChangeResponse();

        _viewModel.OnAffirmative();

        _undoManager.Verify(x => x.RequestLock(), Times.Once);
    }

    [Fact]
    public void RenameCustomVariable_ShouldUpdateInstanceVariableNames()
    {
        var customVariable = new VariableSave
        {
            Name = "OldVar",
            Type = "float",
            IsCustomVariable = true,
            Value = 0f
        };
        _component.DefaultState.Variables.Add(customVariable);

        var screen = new ScreenSave();
        screen.Name = "TestScreen";
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });

        var instance = new InstanceSave
        {
            Name = "myComp",
            BaseType = "TestComponent",
            ParentContainer = screen
        };
        screen.Instances.Add(instance);
        screen.DefaultState.SetValue("myComp.OldVar", 5f, "float");

        ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);

        _viewModel.Variable = customVariable;
        _viewModel.Element = _component;
        _viewModel.EnteredName = "NewVar";
        _viewModel.SelectedItem = "float";
        _viewModel.VariableChangeResponse = new VariableChangeResponse();

        _viewModel.OnAffirmative();

        screen.DefaultState.GetVariableSave("myComp.NewVar").ShouldNotBeNull();
        screen.DefaultState.GetVariableSave("myComp.OldVar").ShouldBeNull();
    }
}
