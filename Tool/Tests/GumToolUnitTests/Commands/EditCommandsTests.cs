using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Moq;

namespace GumToolUnitTests.Commands;

public class EditCommandsTests
{
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<INameVerifier> _nameVerifier = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IProjectCommands> _projectCommands = new();
    
    private readonly EditCommands _editCommands;

    public EditCommandsTests()
    {
        _editCommands = new EditCommands(
            _selectedState.Object,
            _nameVerifier.Object,
            new Mock<IRenameLogic>().Object,
            new Mock<IUndoManager>().Object,
            _dialogService.Object,
            new Mock<IFileCommands>().Object,
            _projectCommands.Object,
            new Mock<IGuiCommands>().Object,
            new Mock<IVariableInCategoryPropagationLogic>().Object
        );
    }
    
    [Fact]
    public void ShowCreateComponentFromInstancesDialog_ShouldIncludeChildren()
    {
        string dummy;
        _nameVerifier
            .Setup(x => x.IsElementNameValid(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ElementSave>(), out dummy))
            .Returns(true);
        _dialogService
            .Setup(x => x.GetUserString(It.IsAny<string>(), "Create Component from selection", It.IsAny<GetUserStringOptions>()))
            .Returns("ComponentName");
        SetupSelectedStateMock();
        
        _editCommands.ShowCreateComponentFromInstancesDialog();
        
        _projectCommands.Verify(
            commands => commands.AddComponent(It.Is<ComponentSave>(comp => VerifyInstancesMatch(comp.Instances))),
            Times.Once
        );
    }
    
    private void SetupSelectedStateMock()
    {
        var component = new ComponentSave { Name = "TestComponent" };
        component.States.Add(new Gum.DataTypes.Variables.StateSave
        {
            Name = "Default"
        });

        var parentInstance = new InstanceSave { Name = "ParentInstance" };
        var childInstance = new InstanceSave { Name = "ChildInstance" };
        component.Instances = [parentInstance, childInstance];
        component.DefaultState.SetValue("ChildInstance.Parent", "ParentInstance", "string");

        component.Instances.ForEach(ins => ins.ParentContainer = component);

        _selectedState
            .Setup(x => x.SelectedElement)
            .Returns(component);
        _selectedState
            .Setup(x => x.SelectedInstances)
            .Returns(component.Instances);
    }
    
    private bool VerifyInstancesMatch(IList<InstanceSave> instances)
    {
        if (instances is not { Count: 2 }) return false;
        var parentInstance = instances.FirstOrDefault(i => i.Name == "ParentInstance");
        var childInstance = instances.FirstOrDefault(i => i.Name == "ChildInstance" &&
                                                          i.GetParentInstance() == parentInstance);

        return parentInstance != null && childInstance != null;
    }
}