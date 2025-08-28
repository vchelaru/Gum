using System.Windows;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Gui.Windows;
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
    private readonly Mock<IWindowDialogService> _windowDialogService = new();
    private readonly Mock<IProjectCommands> _projectCommands = new();

    private readonly ElementSave _elementToEdit;
    private readonly EditCommands _editCommands;

    public EditCommandsTests()
    {
        // Copied part of the selected state setup code from UndoManagerTests. Gotta extract it to an internal static extension method.
        
        var component = new ComponentSave();
        _elementToEdit = component;
        component.States.Add(new Gum.DataTypes.Variables.StateSave 
        {
            Name="Default"
        });

        var parentInstance = new InstanceSave { Name = "ParentInstance" };
        var childInstance = new InstanceSave { Name = "ChildInstance" };
        component.DefaultState.SetValue("ChildInstance.Parent", "ParentInstance", "string");
        component.Instances = [parentInstance, childInstance];
        
        _selectedState
            .Setup(x => x.SelectedElement)
            .Returns(component);
        _selectedState
            .Setup(x => x.SelectedComponent)
            .Returns(component);
        _selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(component.DefaultState);

        var nameVerifier = new Mock<INameVerifier>();
        string dummy;
        nameVerifier
            .Setup(x => x.IsElementNameValid(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ElementSave>(), out dummy))
            .Returns(true);

        _windowDialogService
            .Setup(x => x.ShowDialog(It.Is<Window>(x => x is CreateComponentWindow)))
            .Returns(true);
        
        _editCommands = new EditCommands(
            _selectedState.Object,
            nameVerifier.Object,
            new Mock<IRenameLogic>().Object,
            new Mock<IUndoManager>().Object,
            new Mock<IDialogService>().Object,
            _windowDialogService.Object,
            new Mock<IFileCommands>().Object,
            _projectCommands.Object,
            new Mock<IGuiCommands>().Object,
            new Mock<IVariableInCategoryPropagationLogic>().Object
        );
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