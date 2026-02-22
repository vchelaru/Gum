using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Collections.Generic;
using System.Reflection;

namespace GumToolUnitTests.Services;

public class EditVariableServiceTests : BaseTestClass
{
    private readonly Mock<IUndoManager> _undoManager;
    private readonly Mock<IRenameLogic> _renameLogic;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly EditVariableService _service;
    private readonly IServiceProvider _testServiceProvider;

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

        _service = new EditVariableService(
            _renameLogic.Object,
            _dialogService.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            _undoManager.Object);

        // RenameExposedVariable calls Locator.GetRequiredService<AddVariableViewModel>()
        // internally, so we must register a usable instance to avoid an exception.
        var mocker = new AutoMocker();
        var addVariableVm = mocker.CreateInstance<AddVariableViewModel>();
        var services = new ServiceCollection();
        services.AddSingleton(addVariableVm);
        _testServiceProvider = services.BuildServiceProvider();
        Locator.Register(_testServiceProvider);

        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
    }

    public override void Dispose()
    {
        // Remove the test service provider we registered so it doesn't bleed into other tests.
        var prop = typeof(Locator).GetProperty(
            "ServiceProviders", BindingFlags.NonPublic | BindingFlags.Static)!;
        var providers = (List<IServiceProvider>)prop.GetValue(null)!;
        providers.Remove(_testServiceProvider);

        base.Dispose();
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
