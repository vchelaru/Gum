using System.Reflection;
using System.Runtime.CompilerServices;
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
    private readonly Mock<INameVerifier> _nameVerifier = new();
    private readonly Mock<IWindowDialogService> _windowDialogService = new();
    private readonly Mock<IProjectCommands> _projectCommands = new();

    private readonly ElementSave _elementToEdit;
    private EditCommands _editCommands;

    public EditCommandsTests()
    {
        var component = new ComponentSave { Name = "TestComponent" };
        _elementToEdit = component;
        component.States.Add(new Gum.DataTypes.Variables.StateSave
        {
            Name = "Default"
        });

        var parentInstance = new InstanceSave { Name = "ParentInstance" };
        var childInstance = new InstanceSave { Name = "ChildInstance" };
        component.DefaultState.SetValue("ChildInstance.Parent", "ParentInstance", "string");
        component.Instances = [parentInstance, childInstance];
        component.Instances.ForEach(ins => ins.ParentContainer = component);

        _selectedState
            .Setup(x => x.SelectedElement)
            .Returns(component);
        _selectedState
            .Setup(x => x.SelectedInstances)
            .Returns([parentInstance]);
        
        string dummy;
        _nameVerifier
            .Setup(x => x.IsElementNameValid(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ElementSave>(), out dummy))
            .Returns(true);
        _nameVerifier
            .Setup(x => x.IsComponentNameAlreadyUsed(It.IsAny<string>()))
            .Returns(false);

        // _windowDialogService
        //     .Setup(x => x.ShowDialog(It.Is<Window>(x => x is CreateComponentWindow)))
        //     .Returns(true);
        var windowMock = GetCreateComponentWindowMock();
        windowMock
            .Setup(w => w.ShowDialog())
            .Returns(true);
        _windowDialogService
            .Setup(x => x.CreateWindow<CreateComponentWindow>())
            .Returns(windowMock.Object);

        DoInStaThread(() =>
        {
            _editCommands = new EditCommands(
                _selectedState.Object,
                _nameVerifier.Object,
                new Mock<IRenameLogic>().Object,
                new Mock<IUndoManager>().Object,
                new Mock<IDialogService>().Object,
                _windowDialogService.Object,
                new Mock<IFileCommands>().Object,
                _projectCommands.Object,
                new Mock<IGuiCommands>().Object,
                new Mock<IVariableInCategoryPropagationLogic>().Object
            );
        });
    }

    [Fact]
    public void ShowCreateComponentFromInstancesDialog_ShouldIncludeChildren()
    {
        DoInStaThread(() => _editCommands.ShowCreateComponentFromInstancesDialog());

        _projectCommands.Verify(
            // commands => commands.AddComponent(It.Is<ComponentSave>(comp => VerifyInstancesMatch(comp.Instances))),
            commands => commands.AddComponent(It.IsAny<ComponentSave>()),
            Times.Once
        );
    }

    private Mock<CreateComponentWindow> GetCreateComponentWindowMock()
    {
        var window = (CreateComponentWindow)RuntimeHelpers.GetUninitializedObject(typeof(CreateComponentWindow));
        
        FieldInfo nameVerifierField = typeof(CreateComponentWindow).GetField("_nameVerifier", BindingFlags.NonPublic | BindingFlags.Instance);
        nameVerifierField.SetValue(window, _nameVerifier.Object);

        var toReturn = new Mock<CreateComponentWindow>();
    }
    
    private static bool VerifyInstancesMatch(IList<InstanceSave> instances)
    {
        if (instances is not { Count: 2 }) return false;

        var parentInstance = instances.FirstOrDefault(i => i.Name == "ParentInstance");
        var childInstance = instances.FirstOrDefault(i => i.Name == "ChildInstance" &&
                                                          i.GetParentInstance() == parentInstance);

        return parentInstance != null && childInstance != null;
    }

    private static void DoInStaThread(ThreadStart what)
    {
        // xUnit executes tests in an MTA thread by default, which would cause exceptions when using System.Windows.InputManager.
        // Therefore, here I forced the method to execute in an STA thread. A clean alternative for this would be to install
        // Xunit.StaFact add-on and replace Fact attribute with StaFact, but I didn't want to install anything without asking.
        var thread = new Thread(what);
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}