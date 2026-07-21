using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests;

public class EditVariableServiceTests : BaseTestClass
{
    private readonly Mock<IUndoManager> _undoManager;
    private readonly Mock<IRenameLogic> _renameLogic;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly EditVariableService _service;

    public EditVariableServiceTests()
    {
        _undoManager = new Mock<IUndoManager>();
        _renameLogic = new Mock<IRenameLogic>();
        _dialogService = new Mock<IDialogService>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();

        _renameLogic
            .Setup(x => x.GetChangesForRenamedVariable(
                It.IsAny<IStateContainer>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new VariableChangeResponse());

        _dialogService
            .Setup(x => x.GetUserString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetUserStringOptions>()))
            .Returns("NewExposedName");

        // RenameExposedVariable invokes the injected AddVariableViewModel factory to re-use its
        // variable-reference rename logic, so supply a usable instance via the factory.
        var addVariableVm = new AutoMocker().CreateInstance<AddVariableViewModel>();

        _service = new EditVariableService(
            _renameLogic.Object,
            _dialogService.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            _undoManager.Object,
            () => addVariableVm);

        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
    }

    [Fact]
    public void GetEditVariableMenuLabel_ShouldReturnEditVariable_ForCustomVariable()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };

        VariableSave variable = new VariableSave
        {
            Name = "MyCustomVar",
            IsCustomVariable = true
        };

        string label = _service.GetEditVariableMenuLabel(variable, component);

        label.ShouldBe("Edit Variable [MyCustomVar]");
    }

    [Fact]
    public void GetEditVariableMenuLabel_ShouldReturnNull_WhenNotEditable()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };

        VariableSave variable = new VariableSave
        {
            Name = "MyInstance.SomeVar"
        };

        string label = _service.GetEditVariableMenuLabel(variable, component);

        label.ShouldBeNull();
    }

    [Fact]
    public void GetEditVariableMenuLabel_ShouldReturnRenameVariable_ForExposedVariable()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };

        VariableSave variable = new VariableSave
        {
            Name = "MyInstance.SomeVar",
            ExposedAsName = "MyExposedName"
        };

        string label = _service.GetEditVariableMenuLabel(variable, component);

        label.ShouldBe("Rename Variable [MyExposedName]");
    }

    [Fact]
    public void RenameExposedVariable_ShouldRequestUndoLock()
    {
        var component = new ComponentSave();
        component.Name = "TestComponent";
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });

        var variable = new VariableSave
        {
            Name = "MyInstance.SomeVar",
            ExposedAsName = "OldExposedName"
        };
        component.DefaultState.Variables.Add(variable);

        _service.ShowEditVariableWindow(variable, component);

        _undoManager.Verify(x => x.RequestLock(), Times.Once);
    }
}
