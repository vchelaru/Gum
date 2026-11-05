using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.DataTypes.Serialization;
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
    private readonly Mock<IUndoManager> _undoManager = new();
    private readonly ExposeVariableService _service;

    public ExposeFormsPropertyTests()
    {
        Mock<IUndoManager> undoManager = _undoManager;
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

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ExposeVariable_OnAFormsPropertyNobodyAuthored_ExposesItInTheOwnersDefaultState(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        _selectedState.Setup(x => x.SelectedElement).Returns(world.Row);

        var response = _service.ExposeVariable(world.Inner, property, ExposedFormsProject.ExposedName);

        response.Succeeded.ShouldBeTrue();
        VariableSave exposed = world.Row.DefaultState!.Variables.Single(item => item.Name == "Inner." + property);
        exposed.ExposedAsName.ShouldBe(ExposedFormsProject.ExposedName);
        exposed.Type.ShouldBe(world.Property.Type);
        exposed.SetsValue.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ExposeVariable_OnAFormsPropertyNobodyAuthored_SavesTheSameAsExposingAnAuthoredValue(string behavior, string property)
    {
        ExposedFormsProject unauthored = new ExposedFormsProject(behavior, property);
        _selectedState.Setup(x => x.SelectedElement).Returns(unauthored.Row);
        _service.ExposeVariable(unauthored.Inner, property, ExposedFormsProject.ExposedName).Succeeded.ShouldBeTrue();
        string unauthoredXml = Serialize(unauthored.Row, property);

        ExposedFormsProject authored = new ExposedFormsProject(behavior, property);
        authored.Row.DefaultState!.SetValue("Inner." + property, authored.SampleValue(), authored.Inner, authored.Property.Type);
        _selectedState.Setup(x => x.SelectedElement).Returns(authored.Row);
        _service.ExposeVariable(authored.Inner, property, ExposedFormsProject.ExposedName).Succeeded.ShouldBeTrue();
        VariableSave authoredVariable = authored.Row.DefaultState.Variables.Single(item => item.Name == "Inner." + property);
        authoredVariable.Value = null;
        authoredVariable.SetsValue = false;

        Serialize(authored.Row, property).ShouldBe(unauthoredXml);
    }

    [Fact]
    public void ExposeVariable_OnAFormsPropertyNobodyAuthored_IsOneUndoStep()
    {
        ExposedFormsProject world = new ExposedFormsProject("ButtonBehavior", "IsEnabled");
        _selectedState.Setup(x => x.SelectedElement).Returns(world.Row);

        _service.ExposeVariable(world.Inner, "IsEnabled", ExposedFormsProject.ExposedName);

        _undoManager.Verify(x => x.RequestLock(), Times.Once);
    }

    [Fact]
    public void ExposeVariable_OnAFormsPropertyOfADerivedControl_ExposesIt()
    {
        ExposedFormsProject world = new ExposedFormsProject("ButtonBehavior", "IsEnabled");
        ComponentSave derived = world.AddComponent("Controls/Derived", world.Control.Name);
        InstanceSave inner = ExposedFormsProject.AddInstance(world.Row, "DerivedInner", derived.Name);
        _selectedState.Setup(x => x.SelectedElement).Returns(world.Row);

        _service.ExposeVariable(inner, "IsEnabled", "DerivedEnabled").Succeeded.ShouldBeTrue();

        world.Row.DefaultState!.Variables.Single(item => item.Name == "DerivedInner.IsEnabled")
            .ExposedAsName.ShouldBe("DerivedEnabled");
    }

    [Fact]
    public void ExposeVariable_OnAFormsPropertyInAScreen_ExposesItLikeAnAuthoredOne()
    {
        ExposedFormsProject world = new ExposedFormsProject("ButtonBehavior", "IsEnabled");
        InstanceSave inner = ExposedFormsProject.AddInstance(world.Screen, "ScreenInner", world.Control.Name);
        _selectedState.Setup(x => x.SelectedElement).Returns(world.Screen);

        _service.ExposeVariable(inner, "IsEnabled", "ScreenEnabled").Succeeded.ShouldBeTrue();

        world.Screen.DefaultState!.Variables.Single(item => item.Name == "ScreenInner.IsEnabled")
            .ExposedAsName.ShouldBe("ScreenEnabled");
    }

    [Fact]
    public void ExposeVariable_OnAnExposedFormsPropertyOfANestedInstance_ExposesItAgain()
    {
        ExposedFormsProject world = new ExposedFormsProject("ButtonBehavior", "IsEnabled");
        world.ExposeProperty();
        _selectedState.Setup(x => x.SelectedElement).Returns(world.Screen);

        var response = _service.ExposeVariable(world.RowInstance, ExposedFormsProject.ExposedName, "ScreenValue");

        response.Succeeded.ShouldBeTrue();
        VariableSave exposed = world.Screen.DefaultState!.Variables.Single(item => item.Name == "RowInstance." + ExposedFormsProject.ExposedName);
        exposed.ExposedAsName.ShouldBe("ScreenValue");
        exposed.Type.ShouldBe("bool");
    }

    [Fact]
    public void ExposeVariable_OnAStandardVariableNobodyAuthored_StillExposesIt()
    {
        ExposedFormsProject world = new ExposedFormsProject("ButtonBehavior", "IsEnabled");
        _selectedState.Setup(x => x.SelectedElement).Returns(world.Row);

        _service.ExposeVariable(world.Inner, "Width", "InnerWidth").Succeeded.ShouldBeTrue();

        VariableSave exposed = world.Row.DefaultState!.Variables.Single(item => item.Name == "Inner.Width");
        exposed.ExposedAsName.ShouldBe("InnerWidth");
        exposed.Type.ShouldBe("float");
    }

    [Fact]
    public void ExposeVariable_OnANameNoBehaviorDeclares_IsRefused()
    {
        ExposedFormsProject world = new ExposedFormsProject("ButtonBehavior", "IsEnabled");
        _selectedState.Setup(x => x.SelectedElement).Returns(world.Row);

        var response = _service.ExposeVariable(world.Inner, "Value", "Nope");

        response.DidAttempt.ShouldBeFalse();
        _dialogService.Verify(x => x.ShowMessage("This variable cannot be exposed.", It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()), Times.Once);
        world.Row.DefaultState!.Variables.ShouldBeEmpty();
    }

    private static string Serialize(ElementSave owner, string property)
    {
        VariableSave variable = owner.DefaultState!.Variables.Single(item => item.Name == "Inner." + property);
        using StringWriter writer = new StringWriter();
        GumFileSerializer.GetCompactSerializer(typeof(VariableSave)).Serialize(writer, variable);
        return writer.ToString();
    }
}
