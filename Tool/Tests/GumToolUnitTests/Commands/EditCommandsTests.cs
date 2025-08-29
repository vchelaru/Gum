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
    private readonly Mock<IProjectCommands> _projectCommands = new();
    
    private readonly EditCommands _editCommands;

    public EditCommandsTests()
    {
        SetupSelectedStateMock();
        var nameVerifier = SetupNameVerifierMock();
        
        _editCommands = new EditCommands(
            _selectedState.Object,
            nameVerifier.Object,
            new Mock<IRenameLogic>().Object,
            new Mock<IUndoManager>().Object,
            new Mock<IDialogService>().Object,
            new Mock<IFileCommands>().Object,
            _projectCommands.Object,
            new Mock<IGuiCommands>().Object,
            new Mock<VariableInCategoryPropagationLogic>().Object
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
        component.DefaultState.SetValue("ChildInstance.Parent", "ParentInstance", "string");
        component.Instances = [parentInstance, childInstance];

        component.Instances.ForEach(ins => ins.ParentContainer = component);
    }

    private Mock<INameVerifier> SetupNameVerifierMock()
    {
        var nameVerifierMock = new Mock<INameVerifier>();
        string dummy;
        nameVerifierMock
            .Setup(x => x.IsElementNameValid(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ElementSave>(), out dummy))
            .Returns(true);

        return nameVerifierMock;
    }
    
    [Fact]
    public void ShowCreateComponentFromInstancesDialog_ShouldIncludeChildren()
    {
        _editCommands.ShowCreateComponentFromInstancesDialog();
        
        _projectCommands.Verify(
            commands => commands.AddComponent(It.Is<ComponentSave>(comp => VerifyInstancesMatch(comp.Instances))),
            Times.Once
        );
    }
    
    private static bool VerifyInstancesMatch(IList<InstanceSave> instances)
    {
        if (instances is not { Count: 2 }) return false;
        var parentInstance = instances.FirstOrDefault(i => i.Name == "ParentInstance");
        var childInstance = instances.FirstOrDefault(i => i.Name == "ChildInstance" &&
                                                          i.GetParentInstance() == parentInstance);

        return parentInstance != null && childInstance != null;
    }
}