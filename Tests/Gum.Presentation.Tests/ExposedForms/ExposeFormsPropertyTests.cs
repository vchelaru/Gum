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

namespace Gum.Presentation.Tests.ExposedForms;

/// <summary>
/// Exposing and un-exposing a behavior's Forms property of an inner instance (the Expose Variable command),
/// run through every Forms property the shipped behaviors declare.
/// </summary>
public class ExposeFormsPropertyTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IPluginManager> _pluginManager = new();
    private readonly ExposeVariableService _service;

    public ExposeFormsPropertyTests()
    {
        Mock<IUndoManager> undoManager = new();
        undoManager.Setup(x => x.RequestLock()).Returns(new UndoLock(() => { }));
        Mock<IFileCommands> fileCommands = new();
        Mock<IVariableSaveLogic> variableSaveLogic = new();
        variableSaveLogic
            .Setup(x => x.GetIfVariableIsActive(It.IsAny<VariableSave>(), It.IsAny<ElementSave>(), It.IsAny<InstanceSave>()))
            .Returns(true);

        _service = new ExposeVariableService(
            undoManager.Object,
            new Mock<IGuiCommands>().Object,
            fileCommands.Object,
            new Mock<IRenameLogic>().Object,
            _selectedState.Object,
            new Mock<INameVerifier>().Object,
            _dialogService.Object,
            variableSaveLogic.Object,
            _pluginManager.Object,
            new InstanceOverrideRemover(undoManager.Object, fileCommands.Object, _pluginManager.Object));
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ExposeVariable_OnAFormsPropertyTheInstanceAuthored_ExposesIt(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        world.Row.DefaultState!.SetValue("Inner." + property, world.SampleValue(), world.Inner, world.Property.Type);
        _selectedState.Setup(x => x.SelectedElement).Returns(world.Row);

        var response = _service.ExposeVariable(world.Inner, property, ExposedFormsProject.ExposedName);

        response.Succeeded.ShouldBeTrue();
        world.Row.DefaultState!.Variables.Single(item => item.Name == "Inner." + property).ExposedAsName.ShouldBe(ExposedFormsProject.ExposedName);
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ExposingTheSameFormsPropertyTwice_KeepsOneVariable(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        world.Row.DefaultState!.SetValue("Inner." + property, world.SampleValue(), world.Inner, world.Property.Type);
        _selectedState.Setup(x => x.SelectedElement).Returns(world.Row);

        _service.ExposeVariable(world.Inner, property, ExposedFormsProject.ExposedName);
        _service.ExposeVariable(world.Inner, property, "Renamed");

        world.Row.DefaultState!.Variables.Count(item => item.Name == "Inner." + property).ShouldBe(1);
    }
}
