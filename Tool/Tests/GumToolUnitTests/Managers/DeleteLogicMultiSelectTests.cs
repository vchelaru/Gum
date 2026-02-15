using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace GumToolUnitTests.Managers;

public class DeleteLogicMultiSelectTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly DeleteLogic _deleteLogic;
    private readonly Mock<ProjectCommands> _projectCommands;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<PluginManager> _pluginManager;
    private readonly Mock<WireframeObjectManager> _wireframeObjectManager;
    private readonly Mock<IProjectManager> _projectManager;

    public DeleteLogicMultiSelectTests()
    {
        _mocker = new AutoMocker();
        _projectCommands = _mocker.GetMock<ProjectCommands>();
        _selectedState = _mocker.GetMock<ISelectedState>();
        _dialogService = _mocker.GetMock<IDialogService>();
        _guiCommands = _mocker.GetMock<IGuiCommands>();
        _fileCommands = _mocker.GetMock<IFileCommands>();
        _pluginManager = _mocker.GetMock<PluginManager>();
        _wireframeObjectManager = _mocker.GetMock<WireframeObjectManager>();
        _projectManager = _mocker.GetMock<IProjectManager>();

        _deleteLogic = new DeleteLogic(
            _projectCommands.Object,
            _selectedState.Object,
            _dialogService.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            _pluginManager.Object,
            _wireframeObjectManager.Object,
            _projectManager.Object);

        var gumProject = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = gumProject;
    }

    [Fact]
    public void RemoveInstance_WithChildren_RemovesOnlyInstance()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        var child = AddChild(screen, "Child", "Parent");

        _deleteLogic.RemoveInstance(parent, screen);

        screen.Instances.ShouldNotContain(parent);
        screen.Instances.ShouldContain(child, "Child should still exist after parent removal");
    }

    [Fact]
    public void RemoveParentReferencesToInstance_RemovesParentVariables()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        AddChild(screen, "Child1", "Parent");
        AddChild(screen, "Child2", "Parent");

        _deleteLogic.RemoveParentReferencesToInstance(parent, screen);

        screen.DefaultState.Variables
            .Where(v => v.GetRootName() == "Parent" && v.Value as string == "Parent")
            .ShouldBeEmpty("All parent references to Parent should be removed");
    }

    [Fact]
    public void RemoveParentReferencesToInstance_WithDottedReference_RemovesIt()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        AddChild(screen, "Child", "Parent.Container");

        _deleteLogic.RemoveParentReferencesToInstance(parent, screen);

        screen.DefaultState.Variables
            .Where(v => v.Value is string s && s.StartsWith("Parent."))
            .ShouldBeEmpty("Dotted parent references should be removed");
    }

    private ScreenSave CreateScreenWithInstances(params string[] instanceNames)
    {
        var screen = new ScreenSave();
        screen.Name = "TestScreen";
        
        var defaultState = new StateSave();
        defaultState.ParentContainer = screen;
        defaultState.Name = "Default";
        screen.States.Add(defaultState);

        foreach (var name in instanceNames)
        {
            var instance = new InstanceSave();
            instance.Name = name;
            instance.ParentContainer = screen;
            screen.Instances.Add(instance);
        }

        return screen;
    }

    private InstanceSave AddChild(ScreenSave screen, string childName, string parentName)
    {
        var child = new InstanceSave();
        child.Name = childName;
        child.ParentContainer = screen;
        screen.Instances.Add(child);

        screen.DefaultState.SetValue($"{childName}.Parent", parentName, "string");

        return child;
    }
}
